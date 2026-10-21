using System.Text.Json;
using SiteKit.Core.Data;
using Umbraco.Cms.Core.Events;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Settings;

namespace MyDockerProject.Services.SiteKitAutomate;

public sealed class SiteKitUpdateOperationalRecordSettings
{
    [Field(Label = "Project ID", Description = "SiteKit project containing the record.", SupportsBindings = true)]
    public string ProjectId { get; set; } = string.Empty;

    [Field(Label = "Entity alias", Description = "SiteKit entity to update.", SupportsBindings = true)]
    public string EntityAlias { get; set; } = string.Empty;

    [Field(Label = "Record ID", Description = "UUID of the existing record.", SupportsBindings = true)]
    public string RecordId { get; set; } = string.Empty;

    [Field(Label = "JSON patch", Description = "Object of changed fields. Fields not included in the patch are preserved.", SupportsBindings = true)]
    public string DataJson { get; set; } = "{}";
}

public sealed class SiteKitUpdateOperationalRecordOutput
{
    public string ProjectId { get; set; } = string.Empty;
    public string EntityAlias { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public int UpdatedFieldCount { get; set; }
}

[Action("sitekit.updateOperationalRecord", "Update SiteKit Operational Record",
    Description = "Apply a permission-checked JSON patch to an existing SiteKit business record.",
    Group = "SiteKit", Icon = "icon-edit")]
public sealed class SiteKitUpdateOperationalRecordAction : ActionBase<SiteKitUpdateOperationalRecordSettings, SiteKitUpdateOperationalRecordOutput>
{
    private readonly IServiceScopeFactory _serviceScopes;

    public SiteKitUpdateOperationalRecordAction(ActionInfrastructure infrastructure, IServiceScopeFactory serviceScopes) : base(infrastructure)
    { _serviceScopes = serviceScopes; }

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<SiteKitUpdateOperationalRecordSettings>();
        if (context.ExecutionContext is null || !Guid.TryParse(settings.RecordId, out _))
            return ActionResult.Failed(new ArgumentException("An execution service account and valid record UUID are required."), StepRunErrorCategory.Validation);
        if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(settings.ProjectId) || !SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(settings.EntityAlias))
            return ActionResult.Failed(new ArgumentException("Project ID or entity alias is invalid."), StepRunErrorCategory.Validation);

        try
        {
            await using var scope = _serviceScopes.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var entities = services.GetRequiredService<ISiteKitEntityRegistry>();
            var records = services.GetRequiredService<ISiteKitRecordStore>();
            var permissions = services.GetRequiredService<ISiteKitPermissionRegistry>();
            var users = services.GetRequiredService<Umbraco.Cms.Core.Services.IUserService>();
            var notifications = services.GetRequiredService<IScopedNotificationPublisher>();
            var serviceAccount = await users.GetAsync(context.ExecutionContext.ServiceAccountKey);
            var actor = serviceAccount?.Email;
            if (string.IsNullOrWhiteSpace(actor))
                return ActionResult.Failed(new UnauthorizedAccessException("The automation service account could not be resolved."), StepRunErrorCategory.Authentication);
            var grant = await permissions.GetAsync(settings.ProjectId, settings.EntityAlias, actor, cancellationToken);
            if (grant?.Operations.Contains("update", StringComparer.Ordinal) != true && grant?.Operations.Contains("manage", StringComparer.Ordinal) != true)
                return ActionResult.Failed(new UnauthorizedAccessException("The automation service account has no SiteKit update grant for this entity."), StepRunErrorCategory.Authentication);

            var entity = await entities.GetAsync(settings.ProjectId, settings.EntityAlias, cancellationToken);
            if (entity is null) return ActionResult.Failed(new InvalidOperationException("SiteKit entity was not found."), StepRunErrorCategory.Validation);
            var record = await records.GetAsync(settings.ProjectId, settings.EntityAlias, settings.RecordId, cancellationToken);
            if (record is null) return ActionResult.Failed(new InvalidOperationException("SiteKit record was not found."), StepRunErrorCategory.Validation);
            using var patch = JsonDocument.Parse(settings.DataJson, new JsonDocumentOptions { MaxDepth = 16 });
            if (patch.RootElement.ValueKind != JsonValueKind.Object) return ActionResult.Failed(new ArgumentException("JSON patch must be an object."), StepRunErrorCategory.Validation);
            var merged = record.Data.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
            var changed = 0;
            foreach (var property in patch.RootElement.EnumerateObject())
            {
                if (property.Name == "id") return ActionResult.Failed(new ArgumentException("Record ID cannot be changed by a patch."), StepRunErrorCategory.Validation);
                merged[property.Name] = property.Value.Clone();
                changed++;
            }
            var data = JsonSerializer.SerializeToElement(merged);
            var validationErrors = SiteKitEntityValidator.ValidateRecord(entity, data);
            foreach (var field in entity.Fields.Where(field => field.Type is "relationship" or "relationships" && data.TryGetProperty(field.Alias, out _)))
            {
                var targetAlias = field.RelatedEntity!;
                if (await entities.GetAsync(settings.ProjectId, targetAlias, cancellationToken) is null)
                { validationErrors = validationErrors.Append($"Related entity '{targetAlias}' is not defined.").ToArray(); continue; }
                var ids = field.Type == "relationships" ? data.GetProperty(field.Alias).EnumerateArray().Select(value => value.GetString()!).ToArray() : [data.GetProperty(field.Alias).GetString()!];
                foreach (var id in ids)
                    if (await records.GetAsync(settings.ProjectId, targetAlias, id, cancellationToken) is null)
                        validationErrors = validationErrors.Append($"Relationship '{field.Alias}' references a missing {targetAlias} record.").ToArray();
            }
            if (validationErrors.Count != 0) return ActionResult.Failed(new ArgumentException(string.Join(" ", validationErrors)), StepRunErrorCategory.Validation);
            var updated = await records.SaveAsync(entity, data, actor, record.Id, cancellationToken);
            notifications.Publish(new SiteKitOperationalRecordChangedNotification(settings.ProjectId, settings.EntityAlias, updated.Id, "updated", updated.UpdatedAt));
            return Success(new SiteKitUpdateOperationalRecordOutput
            { ProjectId = settings.ProjectId, EntityAlias = settings.EntityAlias, RecordId = updated.Id, UpdatedFieldCount = changed });
        }
        catch (JsonException exception) { return ActionResult.Failed(exception, StepRunErrorCategory.Validation); }
        catch (UnauthorizedAccessException exception) { return ActionResult.Failed(exception, StepRunErrorCategory.Authentication); }
        catch (ArgumentException exception) { return ActionResult.Failed(exception, StepRunErrorCategory.Validation); }
    }
}
