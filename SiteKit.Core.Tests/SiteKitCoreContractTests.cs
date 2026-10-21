using System.Text.Json;
using SiteKit.Core.Components;
using SiteKit.Core.Data;

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
        using var createData = JsonDocument.Parse("{\"name\":\"Ada\",\"status\":\"lead\"}");

        try
        {
            var created = await store.SaveAsync(entity, createData.RootElement, "admin@example.test");
            var read = await store.GetAsync(entity.ProjectId, entity.Alias, created.Id);
            Assert.NotNull(read);
            Assert.Equal("Ada", read.Data.GetProperty("name").GetString());
            Assert.Equal("admin@example.test", read.CreatedBy);

            using var updateData = JsonDocument.Parse("{\"name\":\"Ada Lovelace\",\"status\":\"customer\"}");
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

    private static SiteKitEntityDefinition CustomerEntity() => new()
    {
        ProjectId = "crm-smoke",
        Alias = "customer",
        Name = "Customer",
        Fields =
        [
            new() { Alias = "id", Name = "Id", Type = "text", Required = true, Unique = true },
            new() { Alias = "name", Name = "Name", Type = "text", Required = true, Unique = true },
            new() { Alias = "status", Name = "Status", Type = "text", Required = true }
        ]
    };
}
