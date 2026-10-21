using System.Text.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using SiteKit.Core.Components;
using SiteKit.Core.Data;
using SiteKit.Core.Importing;
using SiteKit.Core.Modules;
using SiteKit.Core.Projects;
using SiteKit.AgentHost;

namespace SiteKit.Core.Tests;

public sealed class SiteKitCoreContractTests
{
    [Fact]
    public void ChatGptPlanConsentUsesOnlyScopesGrantedByTheTokenResponse()
    {
        Assert.Empty(ChatGptAccount.ParseGrantedScopes(null));
        Assert.Empty(ChatGptAccount.ParseGrantedScopes("  "));
        Assert.Equal(new[] { "openid", "offline_access" }, ChatGptAccount.ParseGrantedScopes("openid offline_access openid"));
        var account = new ChatGptAccount
        {
            Issuer = "https://auth.openai.com", Subject = "subject", ClientId = "client", HostId = "urn:uuid:00000000-0000-0000-0000-000000000001",
            IdToken = "id", AccessToken = "access", RefreshToken = "refresh", Scopes = ChatGptAccount.ParseGrantedScopes(null)
        };
        Assert.False(account.IsEligibleForPlanUsage);
    }

    [Fact]
    public async Task AgentBuildToolRestoresAndBuildsFirstGeneratedProject()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-agent-build", Guid.NewGuid().ToString("N"));
        try
        {
            var projects = new JsonSiteKitProjectRegistry(Path.Combine(root, "MyDockerProject", "MyDockerProject", "umbraco", "Data", "SiteKit", "projects"));
            await projects.SaveAsync(new SiteKitProjectManifest { Id = "first-generated-api", Name = "First Generated API", Category = "api" });
            var tools = new SiteKitToolHost(root);
            using var arguments = JsonDocument.Parse("{\"projectId\":\"first-generated-api\"}");

            using var generated = JsonDocument.Parse(await tools.ExecuteAsync("sitekit_generate_project", arguments.RootElement));
            Assert.True(generated.RootElement.GetProperty("ok").GetBoolean());
            var destination = generated.RootElement.GetProperty("result").GetProperty("directory").GetString()!;
            Assert.True(File.Exists(Path.Combine(destination, "global.json")));

            using var built = JsonDocument.Parse(await tools.ExecuteAsync("sitekit_build_project", arguments.RootElement));
            Assert.True(built.RootElement.GetProperty("ok").GetBoolean(), built.RootElement.ToString());
            Assert.Equal(0, built.RootElement.GetProperty("result").GetProperty("exitCode").GetInt32());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task AdditiveEntitySchemaUpgradePreservesExistingRecordAndRejectsBreakingChanges()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-schema-migration", Guid.NewGuid().ToString("N"));
        try
        {
            var entities = new JsonSiteKitEntityRegistry(Path.Combine(root, "entities"));
            var records = new JsonSiteKitRecordStore(Path.Combine(root, "records"));
            var versionOne = new SiteKitEntityDefinition
            {
                ProjectId = "sales-crm", Alias = "customer", Name = "Customer",
                Fields = [new() { Alias = "id", Name = "Id", Type = "text" }, new() { Alias = "name", Name = "Name", Type = "text", Required = true }]
            };
            await entities.SaveAsync(versionOne);
            using var payload = JsonDocument.Parse("{\"name\":\"Alder & Finch\"}");
            var originalRecord = await records.SaveAsync(versionOne, payload.RootElement, "migration-test@sitekit.local");

            var versionTwo = versionOne with
            {
                Version = 2,
                Fields = [.. versionOne.Fields, new() { Alias = "email", Name = "Email", Type = "email" }]
            };
            await entities.SaveAsync(versionTwo);
            var retainedRecord = await records.GetAsync(versionOne.ProjectId, versionOne.Alias, originalRecord.Id);
            Assert.NotNull(retainedRecord);
            Assert.Equal("Alder & Finch", retainedRecord.Data.GetProperty("name").GetString());
            Assert.False(retainedRecord.Data.TryGetProperty("email", out _));

            var breakingVersion = versionTwo with
            {
                Version = 3,
                Fields = [.. versionTwo.Fields, new() { Alias = "account-code", Name = "Account code", Type = "text", Required = true }]
            };
            await Assert.ThrowsAsync<InvalidOperationException>(() => entities.SaveAsync(breakingVersion));
            var unchangedSchema = await entities.GetAsync(versionOne.ProjectId, versionOne.Alias);
            Assert.Equal(2, unchangedSchema!.Version);
            Assert.Equal(3, unchangedSchema.Fields.Length);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RestImportFetchUsesAnAllowlistedHttpsHost()
    {
        var handler = new StaticHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("api.example.test", request.RequestUri!.Host);
            Assert.Contains(request.Headers.Accept, header => header.MediaType == "application/json");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[{\"email\":\"ada@example.test\"}]") };
        });
        using var client = new HttpClient(handler);
        var source = new SiteKitRestImportSource(client, ["api.example.test"]);

        var bytes = await source.FetchAsync(RestImportMapping());

        Assert.Equal("[{\"email\":\"ada@example.test\"}]", System.Text.Encoding.UTF8.GetString(bytes.Span));
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task RestImportRejectsNonAllowlistedHostsAndRedirects()
    {
        var handler = new StaticHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Redirect));
        using var client = new HttpClient(handler);
        var source = new SiteKitRestImportSource(client, ["api.example.test"]);

        await Assert.ThrowsAsync<ArgumentException>(() => source.FetchAsync(RestImportMapping("https://other.example.test/customers")));
        Assert.Equal(0, handler.RequestCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.FetchAsync(RestImportMapping()));
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task RestImportRejectsOversizedResponsesAndUnsafeSourceUrls()
    {
        var handler = new StaticHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[SiteKitDataImportService.MaximumBytes + 1])
        });
        using var client = new HttpClient(handler);
        var source = new SiteKitRestImportSource(client, ["api.example.test"]);

        await Assert.ThrowsAsync<ArgumentException>(() => source.FetchAsync(RestImportMapping("http://api.example.test/customers")));
        await Assert.ThrowsAsync<ArgumentException>(() => source.FetchAsync(RestImportMapping("https://user:password@api.example.test/customers")));
        Assert.Equal(0, handler.RequestCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.FetchAsync(RestImportMapping()));
        Assert.Equal(1, handler.RequestCount);
    }

    private static SiteKitImportMapping RestImportMapping(string sourceUrl = "https://api.example.test/customers") => new()
    {
        ProjectId = "crm-smoke",
        Alias = "customer-import",
        Name = "Customer import",
        EntityAlias = "customer",
        Fields = new Dictionary<string, string> { ["email"] = "email" },
        MatchFields = ["email"],
        Format = "json",
        SourceUrl = sourceUrl
    };

    private sealed class StaticHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> createResponse) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(createResponse(request));
        }
    }

    [Fact]
    public void ComponentRendererEncodesUntrustedText()
    {
        var component = new SiteKitComponentDefinition
        {
            Alias = "customer-card",
            Name = "Customer Card",
            Category = "business",
            Properties = [new() { Alias = "name", Label = "Name", Type = "text", Required = true }],
            Layout = new() { Type = "heading", Bind = "name", HeadingLevel = 2 }
        };
        using var data = JsonDocument.Parse("{\"name\":\"<script>alert(1)</script>\"}");

        var html = SiteKitComponentRenderer.Render(component, data.RootElement);

        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void ComponentStudioCanAuthorAccessibleInteractiveAccordionPrimitive()
    {
        var component = new SiteKitComponentDefinition
        {
            Alias = "faq-disclosure", Name = "FAQ Disclosure", Category = "content",
            Properties =
            [
                new() { Alias = "question", Label = "Question", Type = "text", Required = true },
                new() { Alias = "answer", Label = "Answer", Type = "text", Required = true }
            ],
            Layout = new()
            {
                Type = "accordion", Bind = "question",
                Children = [new() { Type = "text", Bind = "answer" }]
            }
        };
        using var data = JsonDocument.Parse("{\"question\":\"<script>Q</script>\",\"answer\":\"<script>A</script>\"}");

        var html = SiteKitComponentRenderer.Render(component, data.RootElement);

        Assert.Contains("<div class=\"sitekit-component\"><details class=\"sitekit-accordion\"><summary>&lt;script&gt;Q&lt;/script&gt;</summary>", html);
        Assert.Contains("&lt;script&gt;A&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public async Task CodeFirstComponentSdkBuildsValidDefinitionsAndExportsThemForReuse()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-component-sdk-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var definition = SiteKitComponentDefinitionBuilder
                .Create("reusable-faq-card", "Reusable FAQ Card", "content")
                .Description("An accessible FAQ disclosure component.")
                .Version("1.0.0")
                .Tag("faq")
                .Tag("support")
                .DependsOn("shared-layout")
                .Property("question", "Question", "text", required: true)
                .Property("answer", "Answer", "text", required: true)
                .Variant("compact", "Compact", "compact")
                .Layout(new SiteKitLayoutNode
                {
                    Type = "accordion", Bind = "question",
                    Children = [new() { Type = "text", Bind = "answer" }]
                })
                .Build();

            var sourceRegistry = new JsonSiteKitComponentRegistry(Path.Combine(root, "source"));
            var destinationRegistry = new JsonSiteKitComponentRegistry(Path.Combine(root, "destination"));
            var sharedLayout = SiteKitComponentDefinitionBuilder
                .Create("shared-layout", "Shared layout", "layout")
                .Property("title", "Title", "text", required: true)
                .Build();
            await sourceRegistry.SaveAsync(sharedLayout);
            await sourceRegistry.SaveAsync(definition);
            await using var package = new MemoryStream(await sourceRegistry.ExportAsync(definition.Alias, definition.Version));
            await destinationRegistry.SaveAsync(sharedLayout);
            var imported = await destinationRegistry.ImportAsync(package);

            Assert.Empty(SiteKitComponentValidator.ValidateDefinition(imported));
            Assert.Equal(definition.Tags, imported.Tags);
            Assert.Equal(definition.Dependencies, imported.Dependencies);
            Assert.Equal(definition.Variants, imported.Variants);
            using var data = JsonDocument.Parse("{\"question\":\"How do I reuse this?\",\"answer\":\"Install the exported component package.\"}");
            var rendered = SiteKitComponentRenderer.Render(imported, data.RootElement, "compact");
            Assert.Contains("<details", rendered);
            Assert.Contains("How do I reuse this?", rendered);
            Assert.Contains("Install the exported component package.", rendered);

            var missingDependency = SiteKitComponentDefinitionBuilder
                .Create("missing-dependency", "Missing dependency", "content")
                .Property("title", "Title", "text", required: true)
                .DependsOn("not-installed")
                .Build();
            await Assert.ThrowsAsync<InvalidOperationException>(() => sourceRegistry.SaveAsync(missingDependency));

            var cycle = SiteKitComponentDefinitionBuilder
                .Create("shared-layout", "Shared layout", "layout")
                .Version("1.0.1")
                .Property("title", "Title", "text", required: true)
                .DependsOn("reusable-faq-card")
                .Build();
            await Assert.ThrowsAsync<InvalidOperationException>(() => sourceRegistry.SaveAsync(cycle));
            await Assert.ThrowsAsync<InvalidOperationException>(() => sourceRegistry.DeleteAsync("shared-layout", "1.0.0"));

            Assert.Throws<ArgumentException>(() => SiteKitComponentDefinitionBuilder
                .Create("Invalid alias", "Invalid", "content")
                .Property("title", "Title", "text", required: true)
                .Build());
            Assert.Throws<ArgumentException>(() => SiteKitComponentDefinitionBuilder
                .Create("invalid-dependencies", "Invalid dependencies", "content")
                .Property("title", "Title", "text", required: true)
                .DependsOn("shared-layout")
                .DependsOn("shared-layout")
                .Build());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void KanbanScreenMustBindToDeclaredEntityFields()
    {
        var entity = CustomerEntity();
        var screen = new SiteKitApplicationScreenDefinition
        {
            ProjectId = "crm-smoke",
            Alias = "customer-pipeline",
            Name = "Customer Pipeline",
            Type = "kanban",
            EntityAlias = "customer",
            Columns = ["name", "status"],
            FormFields = ["name"],
            GroupField = "status"
        };

        Assert.Empty(SiteKitScreenValidator.Validate(screen, entity));
        var invalid = SiteKitScreenValidator.Validate(screen with { Columns = ["name", "missing"] }, entity);
        Assert.Contains(invalid, error => error.Contains("not defined", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("1.5.0", "^1.2.0", true)]
    [InlineData("2.0.0", "^1.2.0", false)]
    [InlineData("1.4.9", ">=1.2.0 <1.5.0", true)]
    [InlineData("1.5.0", ">=1.2.0 <1.5.0", false)]
    [InlineData("1.2.0-beta.2", ">=1.2.0-beta.1 <1.2.0", true)]
    [InlineData("1.0.0", "~1.2.0", false)]
    public void ModuleDependencyRangesUseSemVer(string version, string range, bool expected)
        => Assert.Equal(expected, SiteKitModuleVersion.Satisfies(version, range));

    [Fact]
    public async Task JsonRecordStorePersistsCrudAndAuditFields()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-record-tests", Guid.NewGuid().ToString("N"));
        var store = new JsonSiteKitRecordStore(root);
        var entity = CustomerEntity();
        using var createData = JsonDocument.Parse("{\"name\":\"Ada\",\"email\":\"ada@example.test\",\"status\":\"lead\"}");

        try
        {
            var created = await store.SaveAsync(entity, createData.RootElement, "admin@example.test");
            var read = await store.GetAsync(entity.ProjectId, entity.Alias, created.Id);
            Assert.NotNull(read);
            Assert.Equal("Ada", read.Data.GetProperty("name").GetString());
            Assert.Equal("admin@example.test", read.CreatedBy);

            using var updateData = JsonDocument.Parse("{\"name\":\"Ada Lovelace\",\"email\":\"ada@example.test\",\"status\":\"customer\"}");
            var updated = await store.SaveAsync(entity, updateData.RootElement, "admin@example.test", created.Id);
            Assert.Equal("Ada Lovelace", updated.Data.GetProperty("name").GetString());
            Assert.Equal(created.CreatedAt, updated.CreatedAt);

            Assert.True(await store.DeleteAsync(entity.ProjectId, entity.Alias, created.Id, "admin@example.test", [entity]));
            Assert.Null(await store.GetAsync(entity.ProjectId, entity.Alias, created.Id));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task JsonRecordStorePreventsDeletingRecordsReferencedByOtherEntities()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-record-tests", Guid.NewGuid().ToString("N"));
        var store = new JsonSiteKitRecordStore(root);
        var customer = CustomerEntity();
        var opportunity = new SiteKitEntityDefinition
        {
            ProjectId = customer.ProjectId, Alias = "opportunity", Name = "Opportunity",
            Fields = [new() { Alias = "title", Name = "Title", Type = "text", Required = true },
                new() { Alias = "customer", Name = "Customer", Type = "relationship", RelatedEntity = "customer", Required = true }]
        };
        try
        {
            using var customerData = JsonDocument.Parse("{\"name\":\"Ada\",\"email\":\"ada@example.test\",\"status\":\"lead\"}");
            var customerRecord = await store.SaveAsync(customer, customerData.RootElement, "admin@example.test");
            using var opportunityData = JsonDocument.Parse($"{{\"title\":\"New business\",\"customer\":\"{Guid.Parse(customerRecord.Id):D}\"}}");
            var opportunityRecord = await store.SaveAsync(opportunity, opportunityData.RootElement, "admin@example.test");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => store.DeleteAsync(customer.ProjectId, customer.Alias, customerRecord.Id, "admin@example.test", [customer, opportunity]));
            Assert.Contains("references it", exception.Message, StringComparison.Ordinal);
            Assert.NotNull(await store.GetAsync(customer.ProjectId, customer.Alias, customerRecord.Id));
            Assert.True(await store.DeleteAsync(opportunity.ProjectId, opportunity.Alias, opportunityRecord.Id, "admin@example.test", [customer, opportunity]));
            Assert.True(await store.DeleteAsync(customer.ProjectId, customer.Alias, customerRecord.Id, "admin@example.test", [customer, opportunity]));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task WebsiteGeneratorProjectsSharedComponentsWithIndependentPlacementData()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-component-projection-tests", Guid.NewGuid().ToString("N"));
        var projects = new JsonSiteKitProjectRegistry(Path.Combine(root, "projects"));
        var components = new JsonSiteKitComponentRegistry(Path.Combine(root, "components"));
        var placements = new JsonSiteKitPlacementRegistry(Path.Combine(root, "placements"));
        await projects.SaveAsync(new SiteKitProjectManifest { Id = "site-one", Name = "Northstar Advisory", Description = "Independent thinking for your next chapter.", Category = "website", Components = ["shared-banner"] });
        await projects.SaveAsync(new SiteKitProjectManifest { Id = "site-two", Name = "Lumen House", Description = "Spaces made for slower mornings.", Category = "website", Components = ["shared-banner"], ThemeSettings = new() { PrimaryColor = "#b45840", SurfaceColor = "#fffaf4", TextColor = "#32231f", Radius = "20px", FontFamily = "serif" } });
        await components.SaveAsync(new SiteKitComponentDefinition
        {
            Alias = "shared-banner", Name = "Studio Hero", Category = "marketing",
            Properties = [new() { Alias = "eyebrow", Label = "Eyebrow", Type = "text", Required = true }, new() { Alias = "title", Label = "Title", Type = "text", Required = true }, new() { Alias = "intro", Label = "Introduction", Type = "text", Required = true }, new() { Alias = "action", Label = "Action", Type = "link", Required = true }],
            Layout = new() { Type = "card", Children = [new() { Type = "text", Bind = "eyebrow" }, new() { Type = "heading", Bind = "title", HeadingLevel = 2 }, new() { Type = "text", Bind = "intro" }, new() { Type = "button", Bind = "action" }] }
        });
        using var firstData = JsonDocument.Parse("{\"eyebrow\":\"STRATEGY · DESIGN · GROWTH\",\"title\":\"Clarity for what comes next\",\"intro\":\"We help ambitious teams turn complex moments into clear direction and meaningful progress.\",\"action\":{\"url\":\"/contact\",\"label\":\"Start a conversation\"}}");
        using var secondData = JsonDocument.Parse("{\"eyebrow\":\"A SMALLER PACE, A BETTER STAY\",\"title\":\"Room to breathe, space to reconnect\",\"intro\":\"Thoughtful places, considered details, and room to find a rhythm of your own.\",\"action\":{\"url\":\"/stays\",\"label\":\"Explore our stays\"}}");
        await placements.SaveAsync("site-one", new SiteKitComponentPlacement
        {
            Id = "homepage-banner", ComponentAlias = "shared-banner", Data = firstData.RootElement,
            Appearance = new() { BackgroundColor = "#e9f1fa", Padding = "32px", BorderRadius = "24px" }
        });
        await placements.SaveAsync("site-two", new SiteKitComponentPlacement
        {
            Id = "homepage-banner", ComponentAlias = "shared-banner", Data = secondData.RootElement,
            Appearance = new() { BackgroundColor = "#f4e2d2", Padding = "44px", BorderRadius = "24px" }
        });

        try
        {
            var generator = new SiteKitProjectGenerator(Path.Combine(root, "generated"), projects, components: components, placements: placements);
            var first = await generator.GenerateAsync("site-one");
            var second = await generator.GenerateAsync("site-two");
            var firstProgram = await File.ReadAllTextAsync(Path.Combine(first, "Program.cs"));
            var secondProgram = await File.ReadAllTextAsync(Path.Combine(second, "Program.cs"));
            Assert.Contains("Clarity for what comes next", firstProgram);
            Assert.Contains("Room to breathe, space to reconnect", secondProgram);
            Assert.Contains("#e9f1fa", firstProgram);
            Assert.Contains("#f4e2d2", secondProgram);
            Assert.DoesNotContain("Room to breathe, space to reconnect", firstProgram);
            Assert.DoesNotContain("Clarity for what comes next", secondProgram);
            using var build = Process.Start(new ProcessStartInfo("dotnet", "build site-one.csproj --configuration Release --nologo")
            {
                WorkingDirectory = first, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
            })!;
            var stdout = await build.StandardOutput.ReadToEndAsync();
            var stderr = await build.StandardError.ReadToEndAsync();
            await build.WaitForExitAsync();
            Assert.True(build.ExitCode == 0, $"Generated website build failed: {stdout}\n{stderr}");
        }
        finally { if (!string.Equals(Environment.GetEnvironmentVariable("SITEKIT_KEEP_COMPONENT_FIXTURE"), "1", StringComparison.Ordinal) && Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Theory]
    [InlineData("json", "[{\"email\":\"ada@example.test\",\"name\":\"Ada\",\"status\":\"customer\"}]")]
    [InlineData("csv", "email,name,status\nada@example.test,Ada,customer\n")]
    [InlineData("xml", "<rows><row><email>ada@example.test</email><name>Ada</name><status>customer</status></row></rows>")]
    public async Task ImportPreviewAndRunSupportCommonFormatsAndIdempotentUpserts(string format, string input)
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-import-tests", Guid.NewGuid().ToString("N"));
        var entities = new JsonSiteKitEntityRegistry(Path.Combine(root, "entities"));
        var records = new JsonSiteKitRecordStore(Path.Combine(root, "records"));
        var service = new SiteKitDataImportService(entities, records);
        var entity = CustomerEntity();
        await entities.SaveAsync(entity);
        using var importedSource = JsonDocument.Parse("{\"name\":\"Ada Lovelace\",\"email\":\"ada@example.test\",\"status\":\"lead\"}");
        await records.SaveAsync(entity, importedSource.RootElement, "admin@example.test");
        var mapping = new SiteKitImportMapping
        {
            ProjectId = entity.ProjectId, Alias = "customers", Name = "Customers", EntityAlias = entity.Alias,
            Format = format, Fields = new Dictionary<string, string> { ["email"] = "email", ["name"] = "name", ["status"] = "status" },
            MatchFields = ["email"]
        };
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        try
        {
            var preview = await service.PreviewAsync(mapping, bytes);
            Assert.Equal(1, preview.RowsRead);
            Assert.Equal(1, preview.ValidRows);
            Assert.Empty(preview.Errors);

            var result = await service.ImportAsync(mapping, bytes, "admin@example.test");
            Assert.Equal(0, result.Created);
            Assert.Equal(1, result.Updated);
            var saved = Assert.Single(await records.ListAsync(entity.ProjectId, entity.Alias));
            Assert.Equal("Ada", saved.Data.GetProperty("name").GetString());
            Assert.Equal("customer", saved.Data.GetProperty("status").GetString());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task ImportPreviewReportsInvalidRecordsWithoutWriting()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-import-tests", Guid.NewGuid().ToString("N"));
        var entities = new JsonSiteKitEntityRegistry(Path.Combine(root, "entities"));
        var records = new JsonSiteKitRecordStore(Path.Combine(root, "records"));
        var service = new SiteKitDataImportService(entities, records);
        var entity = CustomerEntity();
        await entities.SaveAsync(entity);
        var mapping = new SiteKitImportMapping
        {
            ProjectId = entity.ProjectId, Alias = "customers", Name = "Customers", EntityAlias = entity.Alias,
            Format = "json", Fields = new Dictionary<string, string> { ["email"] = "email", ["status"] = "status" }, MatchFields = ["email"]
        };
        try
        {
            var preview = await service.PreviewAsync(mapping, System.Text.Encoding.UTF8.GetBytes("[{\"email\":\"ada@example.test\",\"status\":\"lead\"}]"));
            Assert.Equal(1, preview.RowsRead);
            Assert.Equal(0, preview.ValidRows);
            Assert.NotEmpty(preview.Errors);
            Assert.Empty(await records.ListAsync(entity.ProjectId, entity.Alias));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task ImportOperationCanBeGrantedExplicitly()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-permission-tests", Guid.NewGuid().ToString("N"));
        var permissions = new JsonSiteKitPermissionRegistry(root);
        try
        {
            var saved = await permissions.SaveAsync(new SiteKitEntityPermissionGrant
            {
                ProjectId = "crm-smoke", EntityAlias = "customer", Principal = "importer@example.test", Operations = ["read", "import"]
            });
            Assert.Contains("import", saved.Operations);
            var loaded = await permissions.GetAsync(saved.ProjectId, saved.EntityAlias, saved.Principal);
            Assert.NotNull(loaded);
            Assert.Equal(saved.ProjectId, loaded.ProjectId);
            Assert.Equal(saved.EntityAlias, loaded.EntityAlias);
            Assert.Equal(saved.Principal, loaded.Principal);
            Assert.Equal(saved.Operations, loaded.Operations);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task ProjectRolesResolveEntityPermissionsForMembersOnly()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-role-tests", Guid.NewGuid().ToString("N"));
        var permissions = new JsonSiteKitPermissionRegistry(Path.Combine(root, "permissions"));
        var roles = new JsonSiteKitProjectRoleRegistry(Path.Combine(root, "roles"));
        var evaluator = new SiteKitPermissionEvaluator(permissions, roles);
        try
        {
            await roles.SaveAsync(new SiteKitProjectRole
            { ProjectId = "crm-smoke", Alias = "sales-team", Name = "Sales team", Members = ["ALEX@example.test", "jules@example.test"] });
            await permissions.SaveAsync(new SiteKitEntityPermissionGrant
            { ProjectId = "crm-smoke", EntityAlias = "opportunity", Principal = "role:sales-team", Operations = ["read", "update"] });

            Assert.True(await evaluator.CanAsync("crm-smoke", "opportunity", "alex@example.test", "update"));
            Assert.True((await evaluator.GetOperationsAsync("crm-smoke", "opportunity", "jules@example.test")).Contains("read"));
            Assert.False(await evaluator.CanAsync("crm-smoke", "opportunity", "outsider@example.test", "read"));
            Assert.False(await evaluator.CanAsync("another-project", "opportunity", "alex@example.test", "read"));
            Assert.False(await evaluator.CanAsync("crm-smoke", "customer", "alex@example.test", "read"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task ImportPreviewContinuesAfterFieldConversionError()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-import-tests", Guid.NewGuid().ToString("N"));
        var entities = new JsonSiteKitEntityRegistry(Path.Combine(root, "entities"));
        var records = new JsonSiteKitRecordStore(Path.Combine(root, "records"));
        var service = new SiteKitDataImportService(entities, records);
        var entity = new SiteKitEntityDefinition
        {
            ProjectId = "crm-smoke", Alias = "invoice", Name = "Invoice",
            Fields = [new() { Alias = "id", Name = "Id", Type = "text", Required = true, Unique = true },
                new() { Alias = "reference", Name = "Reference", Type = "text", Required = true, Unique = true },
                new() { Alias = "total", Name = "Total", Type = "number", Required = true }]
        };
        await entities.SaveAsync(entity);
        var mapping = new SiteKitImportMapping
        {
            ProjectId = entity.ProjectId, Alias = "invoices", Name = "Invoices", EntityAlias = entity.Alias,
            Format = "json", Fields = new Dictionary<string, string> { ["reference"] = "reference", ["total"] = "total" }, MatchFields = ["reference"]
        };
        try
        {
            var preview = await service.PreviewAsync(mapping, System.Text.Encoding.UTF8.GetBytes("[{\"reference\":\"INV-1\",\"total\":\"12.5\"},{\"reference\":\"INV-2\",\"total\":\"not-a-number\"}]"));
            Assert.Equal(2, preview.RowsRead);
            Assert.Equal(1, preview.ValidRows);
            Assert.Contains(preview.Errors, error => error.Row == 2 && error.Error.Contains("cannot be converted", StringComparison.Ordinal));
            Assert.Single(preview.Samples);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task ImportRunHistoryPersistsValueFreeAuditMetadata()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-import-run-tests", Guid.NewGuid().ToString("N"));
        var registry = new JsonSiteKitImportRunRegistry(root);
        var started = DateTimeOffset.UtcNow;
        var run = new SiteKitImportRun
        {
            Id = Guid.NewGuid().ToString("N"), ProjectId = "crm-smoke", MappingAlias = "customers", EntityAlias = "customer",
            Actor = "admin@example.test", Mode = "import", Status = "partial", ContentSha256 = new string('A', 64),
            RowsRead = 4, Created = 2, Updated = 1, ErrorCount = 1, StartedAt = started, FinishedAt = started.AddSeconds(1)
        };
        try
        {
            await registry.SaveAsync(run);
            var stored = Assert.Single(await registry.ListAsync(run.ProjectId, run.MappingAlias));
            Assert.Equal(run.Id, stored.Id);
            Assert.Equal("admin@example.test", stored.Actor);
            Assert.Equal(1, stored.ErrorCount);
            var json = await File.ReadAllTextAsync(Path.Combine(root, run.ProjectId, run.MappingAlias, run.Id + ".json"));
            Assert.DoesNotContain("sensitive customer value", json, StringComparison.Ordinal);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task ModuleVersionsArePinnedAndOnlyMoveForwardThroughUpgrade()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-module-upgrade-tests", Guid.NewGuid().ToString("N"));
        var packages = Path.Combine(root, "modules");
        var source = Path.Combine(root, "source");
        var zip = Path.Combine(root, "packages", "module.zip");
        var projects = new JsonSiteKitProjectRegistry(Path.Combine(root, "projects"));
        var components = new JsonSiteKitComponentRegistry(Path.Combine(root, "components"));
        var entities = new JsonSiteKitEntityRegistry(Path.Combine(root, "entities"));
        var records = new JsonSiteKitRecordStore(Path.Combine(root, "records"));
        var screens = new JsonSiteKitScreenRegistry(Path.Combine(root, "screens"));
        var modules = new JsonSiteKitModuleRegistry(packages);
        var installer = new SiteKitModuleInstaller(packages, modules, projects, components, entities, screens);
        await projects.SaveAsync(new SiteKitProjectManifest { Id = "sales-crm", Name = "Sales CRM", Category = "application" });

        try
        {
            await ImportModule("1.0.0");
            var first = await installer.InstallAsync("sales-crm", "sales-module", "1.0.0");
            Assert.Equal("1.0.0", first.ModuleVersions["sales-module"]);

            await ImportModule("1.1.0");
            await Assert.ThrowsAsync<InvalidOperationException>(() => installer.InstallAsync("sales-crm", "sales-module", "1.1.0"));
            var upgraded = await installer.UpgradeAsync("sales-crm", "sales-module", "1.1.0");
            Assert.Equal("1.1.0", upgraded.ModuleVersions["sales-module"]);
            await Assert.ThrowsAsync<InvalidOperationException>(() => installer.UpgradeAsync("sales-crm", "sales-module", "1.0.0"));

            await ImportModule("1.0.0", "sales-addon", "addon-card", ["sales-module"], new Dictionary<string, string> { ["sales-module"] = "<1.0.0" });
            await Assert.ThrowsAsync<InvalidOperationException>(() => installer.InstallAsync("sales-crm", "sales-addon", "1.0.0"));
            await ImportModule("1.0.1", "sales-addon", "addon-card", ["sales-module"], new Dictionary<string, string> { ["sales-module"] = ">=1.0.0 <2.0.0" });
            var installedAddon = await installer.InstallAsync("sales-crm", "sales-addon", "1.0.1");
            Assert.Equal("1.0.1", installedAddon.ModuleVersions["sales-addon"]);
            var customer = await entities.GetAsync("sales-crm", "customer");
            var customerId = Guid.NewGuid();
            using var customerData = JsonDocument.Parse(JsonSerializer.Serialize(new { id = customerId.ToString(), name = "Preserved module customer" }));
            var savedCustomer = await records.SaveAsync(customer!, customerData.RootElement, "admin@example.test", customerId.ToString());
            await ImportModule("2.0.0");
            await Assert.ThrowsAsync<InvalidOperationException>(() => installer.UpgradeAsync("sales-crm", "sales-module", "2.0.0"));
            Assert.Equal("1.1.0", (await projects.GetAsync("sales-crm"))!.ModuleVersions["sales-module"]);

            await Assert.ThrowsAsync<InvalidOperationException>(() => installer.UninstallAsync("sales-crm", "sales-module"));
            var withoutAddon = await installer.UninstallAsync("sales-crm", "sales-addon");
            Assert.DoesNotContain("sales-addon", withoutAddon.Modules);
            Assert.DoesNotContain("sales-addon", withoutAddon.ModuleVersions.Keys);
            Assert.Contains("addon-card", withoutAddon.Components);
            Assert.Contains("crm-card", withoutAddon.Components);
            Assert.Contains("customer", withoutAddon.Entities);
            var withoutBaseModule = await installer.UninstallAsync("sales-crm", "sales-module");
            Assert.Empty(withoutBaseModule.Modules);
            Assert.Empty(withoutBaseModule.ModuleVersions);
            Assert.Contains("crm-card", withoutBaseModule.Components);
            Assert.Contains("customer", withoutBaseModule.Entities);
            Assert.Equal("Preserved module customer", (await records.GetAsync("sales-crm", "customer", savedCustomer.Id))!.Data.GetProperty("name").GetString());
            await Assert.ThrowsAsync<InvalidOperationException>(() => installer.UninstallAsync("sales-crm", "sales-module"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }

        async Task ImportModule(string version, string moduleId = "sales-module", string componentAlias = "crm-card", string[]? dependencies = null, Dictionary<string, string>? dependencyVersions = null)
        {
            if (Directory.Exists(source)) Directory.Delete(source, recursive: true);
            Directory.CreateDirectory(Path.Combine(source, "components"));
            Directory.CreateDirectory(Path.Combine(source, "entities"));
            var component = new SiteKitComponentDefinition
            {
                Alias = componentAlias, Name = "CRM Card", Category = "Business", Version = version,
                Properties = [new() { Alias = "title", Label = "Title", Type = "text", Required = true }],
                Layout = new() { Type = "heading", Bind = "title", HeadingLevel = 2 }
            };
            await File.WriteAllBytesAsync(Path.Combine(source, "components", "crm-card.json"), JsonSerializer.SerializeToUtf8Bytes(component));
            var entity = new SiteKitEntityDefinition
            {
                ProjectId = "module-package", Alias = "customer", Name = "Customer",
                Fields = [new() { Alias = "id", Name = "Id", Type = "text", Required = true, Unique = true }, new() { Alias = "name", Name = "Name", Type = "text", Required = true }]
            };
            await File.WriteAllBytesAsync(Path.Combine(source, "entities", "customer.json"), JsonSerializer.SerializeToUtf8Bytes(entity));
            var manifest = new SiteKitModuleManifest
            {
                Id = moduleId, Name = "Sales Module", Version = version,
                CompatibleSiteKitVersions = ["2.0.0"], ProjectCategories = ["application"],
                Dependencies = dependencies ?? [], DependencyVersions = dependencyVersions ?? new Dictionary<string, string>()
            };
            await File.WriteAllBytesAsync(Path.Combine(source, "sitekit.module.json"), JsonSerializer.SerializeToUtf8Bytes(manifest));
            await SiteKitModulePackageBuilder.BuildAsync(source, zip);
            await using var package = File.OpenRead(zip);
            await modules.ImportAsync(package);
        }
    }

    [Fact]
    public async Task ApplicationProjectGenerationIncludesScreenRuntimeAndProtectedCrud()
    {
        var root = Path.Combine(Path.GetTempPath(), "sitekit-generator-tests", Guid.NewGuid().ToString("N"));
        var preserveFixture = string.Equals(Environment.GetEnvironmentVariable("SITEKIT_KEEP_GENERATOR_FIXTURE"), "1", StringComparison.Ordinal);
        var projects = new JsonSiteKitProjectRegistry(Path.Combine(root, "projects"));
        var entities = new JsonSiteKitEntityRegistry(Path.Combine(root, "entities"));
        var screens = new JsonSiteKitScreenRegistry(Path.Combine(root, "screens"));
        var project = new SiteKitProjectManifest { Id = "sales-crm", Name = "Sales CRM", Description = "A clear view of your customer relationships and the opportunities ahead.", Category = "application", Entities = ["customer", "opportunity"], Screens = ["sales-overview", "customers", "opportunity-pipeline"] };
        await projects.SaveAsync(project);
        await entities.SaveAsync(new SiteKitEntityDefinition
        {
            ProjectId = project.Id, Alias = "customer", Name = "Customer",
            Fields = [new() { Alias = "id", Name = "Id", Type = "text", Required = true, Unique = true }, new() { Alias = "name", Name = "Name", Type = "text", Required = true }]
        });
        await entities.SaveAsync(new SiteKitEntityDefinition
        {
            ProjectId = project.Id, Alias = "opportunity", Name = "Opportunity",
            Fields = [new() { Alias = "id", Name = "Id", Type = "text", Required = true, Unique = true }, new() { Alias = "title", Name = "Title", Type = "text", Required = true }, new() { Alias = "stage", Name = "Stage", Type = "text", Required = true }, new() { Alias = "customer", Name = "Customer", Type = "relationship", RelatedEntity = "customer", Required = true }]
        });
        await screens.SaveAsync(new SiteKitApplicationScreenDefinition
        {
            ProjectId = project.Id, Alias = "sales-overview", Name = "Sales Overview", Type = "dashboard",
            Columns = [], FormFields = [], Components = []
        });
        await screens.SaveAsync(new SiteKitApplicationScreenDefinition
        {
            ProjectId = project.Id, Alias = "customers", Name = "Customers", Type = "table", EntityAlias = "customer",
            Columns = ["name"], FormFields = ["name"]
        });
        var pipelineScreen = new SiteKitApplicationScreenDefinition
        {
            ProjectId = project.Id, Alias = "opportunity-pipeline", Name = "Opportunity Pipeline", Type = "kanban", EntityAlias = "opportunity",
            Columns = ["title", "stage"], FormFields = ["title", "stage"], GroupField = "stage"
        };
        await screens.SaveAsync(pipelineScreen);
        try
        {
            var generator = new SiteKitProjectGenerator(Path.Combine(root, "generated"), projects, screens, entities);
            var destination = await generator.GenerateAsync(project.Id);
            var program = await File.ReadAllTextAsync(Path.Combine(destination, "Program.cs"));
            Assert.Contains("MapPost(\"/api/sitekit/records/{entity}\"", program);
            Assert.Contains("SITEKIT_ADMIN_TOKEN", program);
            Assert.Contains("Normalize(Entity model", program);
            var screenRuntime = await File.ReadAllTextAsync(Path.Combine(destination, "wwwroot", "sitekit-runtime.js"));
            Assert.Contains("screen.type==='kanban'", screenRuntime);
            Assert.Contains("screen.type==='dashboard'", screenRuntime);
            Assert.Contains("data-edit", screenRuntime);
            Assert.Contains("method:recordId?'PUT':'POST'", screenRuntime);
            Assert.Contains("Save changes", screenRuntime);
            Assert.Contains("relatedEntity=entities.find", screenRuntime);
            Assert.Contains("selectedOptions", screenRuntime);
            Assert.True(File.Exists(Path.Combine(destination, "sitekit.screens.json")));
            var generatedScreenOrder = JsonSerializer.Deserialize<SiteKitApplicationScreenDefinition[]>(await File.ReadAllTextAsync(Path.Combine(destination, "sitekit.screens.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.Equal(project.Screens, generatedScreenOrder!.Select(screen => screen.Alias));
            Assert.True(File.Exists(Path.Combine(destination, "sitekit.entities.json")));
            using var build = Process.Start(new ProcessStartInfo("dotnet", $"build \"{project.Id}.csproj\" --configuration Release --nologo")
            {
                WorkingDirectory = destination, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
            })!;
            var stdout = await build.StandardOutput.ReadToEndAsync();
            var stderr = await build.StandardError.ReadToEndAsync();
            await build.WaitForExitAsync();
            Assert.True(build.ExitCode == 0, $"Generated project build failed: {stdout}\n{stderr}");

            var portProbe = new TcpListener(IPAddress.Loopback, 0);
            portProbe.Start();
            var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
            portProbe.Stop();
            const string token = "sitekit-generator-integration-test-token";
            const string readerToken = "sitekit-generator-readonly-test-token";
            const string roleToken = "sitekit-generator-role-test-token";
            var readerHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(readerToken)));
            var roleHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(roleToken)));
            var principals = JsonSerializer.Serialize(new Dictionary<string, string> { [readerHash] = "sales-reader", [roleHash] = "sales-manager" });
            const string principalRoles = "{\"sales-manager\":[\"sales-team\"]}";
            const string grants = "{\"sales-reader\":{\"customer\":[\"read\"]},\"role:sales-team\":{\"customer\":[\"read\"]}}";
            using var app = Process.Start(new ProcessStartInfo("dotnet", $"run --no-build --configuration Release --project \"{project.Id}.csproj\" -- --urls http://127.0.0.1:{port}")
            {
                WorkingDirectory = destination, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                Environment = { ["SITEKIT_ADMIN_TOKEN"] = token, ["SITEKIT_PRINCIPAL_TOKENS"] = principals, ["SITEKIT_PRINCIPAL_ROLES"] = principalRoles, ["SITEKIT_ENTITY_GRANTS"] = grants, ["Logging__LogLevel__Default"] = "Warning" }
            })!;
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(2) };
                var ready = false;
                for (var attempt = 0; attempt < 40 && !ready; attempt++)
                {
                    try { ready = (await client.GetAsync("/health")).IsSuccessStatusCode; }
                    catch (HttpRequestException) { await Task.Delay(150); }
                    catch (TaskCanceledException) { await Task.Delay(150); }
                    if (app.HasExited) break;
                }
                Assert.True(ready, "Generated app did not start within the startup window.");
                var unauthorized = await client.GetAsync("/api/sitekit/records/customer");
                Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using (var readerClient = new HttpClient { BaseAddress = client.BaseAddress })
                {
                    readerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", readerToken);
                    Assert.Equal(HttpStatusCode.OK, (await readerClient.GetAsync("/api/sitekit/records/customer")).StatusCode);
                    Assert.Equal(HttpStatusCode.Forbidden, (await readerClient.PostAsJsonAsync("/api/sitekit/records/customer", new { name = "Not allowed" })).StatusCode);
                }
                using (var roleClient = new HttpClient { BaseAddress = client.BaseAddress })
                {
                    roleClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", roleToken);
                    Assert.Equal(HttpStatusCode.OK, (await roleClient.GetAsync("/api/sitekit/records/customer")).StatusCode);
                    Assert.Equal(HttpStatusCode.Forbidden, (await roleClient.PostAsJsonAsync("/api/sitekit/records/customer", new { name = "Role member cannot create without a grant" })).StatusCode);
                }
                var createdResponse = await client.PostAsJsonAsync("/api/sitekit/records/customer", new { name = "Ada Lovelace" });
                Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
                using var created = JsonDocument.Parse(await createdResponse.Content.ReadAsStringAsync());
                var id = created.RootElement.GetProperty("id").GetString()!;
                Assert.Equal(id, created.RootElement.GetProperty("data").GetProperty("id").GetString());
                Assert.True(File.Exists(Path.Combine(destination, "App_Data", "sitekit.db")));
                var updateResponse = await client.PutAsJsonAsync($"/api/sitekit/records/customer/{id}", new { name = "Ada Byron" });
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var invalidRelationship = await client.PostAsJsonAsync("/api/sitekit/records/opportunity", new { title = "Invalid", stage = "Lead", customer = "00000000000000000000000000000000" });
                Assert.Equal(HttpStatusCode.BadRequest, invalidRelationship.StatusCode);
                var opportunityResponse = await client.PostAsJsonAsync("/api/sitekit/records/opportunity", new { title = "Renewal", stage = "Qualified", customer = id });
                Assert.Equal(HttpStatusCode.Created, opportunityResponse.StatusCode);
                using var opportunity = JsonDocument.Parse(await opportunityResponse.Content.ReadAsStringAsync());
                var opportunityId = opportunity.RootElement.GetProperty("id").GetString()!;
                Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/sitekit/records/customer/{id}")).StatusCode);
                app.Kill(entireProcessTree: true);
                await app.WaitForExitAsync();
                await screens.SaveAsync(pipelineScreen with { Name = "Sales Pipeline" });
                await generator.GenerateAsync(project.Id);
                using var restarted = Process.Start(new ProcessStartInfo("dotnet", $"run --no-build --configuration Release --project \"{project.Id}.csproj\" -- --urls http://127.0.0.1:{port}")
                {
                    WorkingDirectory = destination, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                    Environment = { ["SITEKIT_ADMIN_TOKEN"] = token, ["SITEKIT_PRINCIPAL_TOKENS"] = principals, ["SITEKIT_PRINCIPAL_ROLES"] = principalRoles, ["SITEKIT_ENTITY_GRANTS"] = grants, ["Logging__LogLevel__Default"] = "Warning" }
                })!;
                try
                {
                    var recovered = false;
                    for (var attempt = 0; attempt < 40 && !recovered; attempt++)
                    {
                        try { recovered = (await client.GetAsync("/health")).IsSuccessStatusCode; }
                        catch (HttpRequestException) { await Task.Delay(150); }
                    }
                    Assert.True(recovered, "Generated app did not restart.");
                    var updatedScreens = await client.GetFromJsonAsync<JsonElement[]>("/api/sitekit/screens");
                    Assert.Contains(updatedScreens!, screen => screen.GetProperty("name").GetString() == "Sales Pipeline");
                    var listed = await client.GetFromJsonAsync<JsonElement[]>("/api/sitekit/records/customer");
                    Assert.Equal("Ada Byron", listed![0].GetProperty("data").GetProperty("name").GetString());
                    var opportunities = await client.GetFromJsonAsync<JsonElement[]>("/api/sitekit/records/opportunity");
                    Assert.Equal(id, opportunities![0].GetProperty("data").GetProperty("customer").GetString());
                    if (!preserveFixture)
                    {
                        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/sitekit/records/opportunity/{opportunityId}")).StatusCode);
                        var deleteResponse = await client.DeleteAsync($"/api/sitekit/records/customer/{id}");
                        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
                    }
                }
                finally
                {
                    if (!restarted.HasExited) restarted.Kill(entireProcessTree: true);
                    await restarted.WaitForExitAsync();
                }
            }
            finally
            {
                if (!app.HasExited) app.Kill(entireProcessTree: true);
                await app.WaitForExitAsync();
            }
        }
        finally { if (!preserveFixture && Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    private static SiteKitEntityDefinition CustomerEntity() => new()
    {
        ProjectId = "crm-smoke",
        Alias = "customer",
        Name = "Customer",
        Fields =
        [
            new() { Alias = "id", Name = "Id", Type = "text", Required = true, Unique = true },
            new() { Alias = "name", Name = "Name", Type = "text", Required = true, Unique = true },
            new() { Alias = "email", Name = "Email", Type = "email", Required = true, Unique = true },
            new() { Alias = "status", Name = "Status", Type = "text", Required = true }
        ]
    };
}
