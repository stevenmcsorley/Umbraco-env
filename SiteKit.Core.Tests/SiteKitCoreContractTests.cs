using System.Text.Json;
using SiteKit.Core.Components;
using SiteKit.Core.Data;
using SiteKit.Core.Importing;

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
