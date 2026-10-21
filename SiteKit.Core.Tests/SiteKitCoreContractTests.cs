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

namespace SiteKit.Core.Tests;

public sealed class SiteKitCoreContractTests
{
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

            Assert.True(await store.DeleteAsync(entity.ProjectId, entity.Alias, created.Id, "admin@example.test"));
            Assert.Null(await store.GetAsync(entity.ProjectId, entity.Alias, created.Id));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
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
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }

        async Task ImportModule(string version)
        {
            if (Directory.Exists(source)) Directory.Delete(source, recursive: true);
            Directory.CreateDirectory(Path.Combine(source, "components"));
            var component = new SiteKitComponentDefinition
            {
                Alias = "crm-card", Name = "CRM Card", Category = "Business", Version = version,
                Properties = [new() { Alias = "title", Label = "Title", Type = "text", Required = true }],
                Layout = new() { Type = "heading", Bind = "title", HeadingLevel = 2 }
            };
            await File.WriteAllBytesAsync(Path.Combine(source, "components", "crm-card.json"), JsonSerializer.SerializeToUtf8Bytes(component));
            var manifest = new SiteKitModuleManifest
            {
                Id = "sales-module", Name = "Sales Module", Version = version,
                CompatibleSiteKitVersions = ["2.0.0"], ProjectCategories = ["application"]
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
        var projects = new JsonSiteKitProjectRegistry(Path.Combine(root, "projects"));
        var entities = new JsonSiteKitEntityRegistry(Path.Combine(root, "entities"));
        var screens = new JsonSiteKitScreenRegistry(Path.Combine(root, "screens"));
        var project = new SiteKitProjectManifest { Id = "sales-crm", Name = "Sales CRM", Category = "application", Entities = ["customer"], Screens = ["customers"] };
        await projects.SaveAsync(project);
        await entities.SaveAsync(new SiteKitEntityDefinition
        {
            ProjectId = project.Id, Alias = "customer", Name = "Customer",
            Fields = [new() { Alias = "id", Name = "Id", Type = "text", Required = true, Unique = true }, new() { Alias = "name", Name = "Name", Type = "text", Required = true }]
        });
        await screens.SaveAsync(new SiteKitApplicationScreenDefinition
        {
            ProjectId = project.Id, Alias = "customers", Name = "Customers", Type = "table", EntityAlias = "customer",
            Columns = ["name"], FormFields = ["name"]
        });
        try
        {
            var destination = await new SiteKitProjectGenerator(Path.Combine(root, "generated"), projects, screens, entities).GenerateAsync(project.Id);
            var program = await File.ReadAllTextAsync(Path.Combine(destination, "Program.cs"));
            Assert.Contains("MapPost(\"/api/sitekit/records/{entity}\"", program);
            Assert.Contains("SITEKIT_ADMIN_TOKEN", program);
            Assert.Contains("Normalize(Entity model", program);
            Assert.True(File.Exists(Path.Combine(destination, "wwwroot", "sitekit-runtime.js")));
            Assert.True(File.Exists(Path.Combine(destination, "sitekit.screens.json")));
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
            var readerHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(readerToken)));
            var principals = JsonSerializer.Serialize(new Dictionary<string, string> { [readerHash] = "sales-reader" });
            const string grants = "{\"sales-reader\":{\"customer\":[\"read\"]}}";
            using var app = Process.Start(new ProcessStartInfo("dotnet", $"run --no-build --configuration Release --project \"{project.Id}.csproj\" -- --urls http://127.0.0.1:{port}")
            {
                WorkingDirectory = destination, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                Environment = { ["SITEKIT_ADMIN_TOKEN"] = token, ["SITEKIT_PRINCIPAL_TOKENS"] = principals, ["SITEKIT_ENTITY_GRANTS"] = grants, ["Logging__LogLevel__Default"] = "Warning" }
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
                var createdResponse = await client.PostAsJsonAsync("/api/sitekit/records/customer", new { name = "Ada Lovelace" });
                Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
                using var created = JsonDocument.Parse(await createdResponse.Content.ReadAsStringAsync());
                var id = created.RootElement.GetProperty("id").GetString()!;
                Assert.Equal(id, created.RootElement.GetProperty("data").GetProperty("id").GetString());
                Assert.True(File.Exists(Path.Combine(destination, "App_Data", "sitekit.db")));
                var updateResponse = await client.PutAsJsonAsync($"/api/sitekit/records/customer/{id}", new { name = "Ada Byron" });
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                app.Kill(entireProcessTree: true);
                await app.WaitForExitAsync();
                using var restarted = Process.Start(new ProcessStartInfo("dotnet", $"run --no-build --configuration Release --project \"{project.Id}.csproj\" -- --urls http://127.0.0.1:{port}")
                {
                    WorkingDirectory = destination, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                    Environment = { ["SITEKIT_ADMIN_TOKEN"] = token, ["SITEKIT_PRINCIPAL_TOKENS"] = principals, ["SITEKIT_ENTITY_GRANTS"] = grants, ["Logging__LogLevel__Default"] = "Warning" }
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
                    var listed = await client.GetFromJsonAsync<JsonElement[]>("/api/sitekit/records/customer");
                    Assert.Equal("Ada Byron", listed![0].GetProperty("data").GetProperty("name").GetString());
                    var deleteResponse = await client.DeleteAsync($"/api/sitekit/records/customer/{id}");
                    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
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
        finally { if (!string.Equals(Environment.GetEnvironmentVariable("SITEKIT_KEEP_GENERATOR_FIXTURE"), "1", StringComparison.Ordinal) && Directory.Exists(root)) Directory.Delete(root, recursive: true); }
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
