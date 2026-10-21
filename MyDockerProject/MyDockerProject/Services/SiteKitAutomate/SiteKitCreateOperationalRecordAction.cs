using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using SiteKit.Core.Data;
using SiteKit.Core.Projects;
using Umbraco.Cms.Core.Events;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Settings;

namespace MyDockerProject.Services.SiteKitAutomate;

public sealed class SiteKitCreateOperationalRecordSettings
{
    [Field(Label = "Project ID", Description = "SiteKit project that owns the record.", SupportsBindings = true)]
    public string ProjectId { get; set; } = string.Empty;

    [Field(Label = "Entity alias", Description = "SiteKit entity to create a record for.", SupportsBindings = true)]
    public string EntityAlias { get; set; } = string.Empty;

    [Field(Label = "Record data (JSON)", Description = "JSON object containing the new record's editable fields.", SupportsBindings = true)]
    public string DataJson { get; set; } = "{}";

    [Field(Label = "Idempotency key", Description = "Stable unique key for this business operation. Replays with the same key return the same record.", SupportsBindings = true)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class SiteKitCreateOperationalRecordOutput
{
    public string ProjectId { get; set; } = string.Empty;
    public string EntityAlias { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public bool AlreadyCreated { get; set; }
}

[Action("sitekit.createOperationalRecord", "Create SiteKit Operational Record",
    Description = "Create a permission-checked SiteKit business record and publish its change event.",
    Group = "SiteKit", Icon = "icon-add")]
public sealed class SiteKitCreateOperationalRecordAction : ActionBase<SiteKitCreateOperationalRecordSettings, SiteKitCreateOperationalRecordOutput>
{
    private const int MaximumJsonLength = 1_048_576;
    private readonly IServiceScopeFactory _serviceScopes;

    public SiteKitCreateOperationalRecordAction(ActionInfrastructure infrastructure, IServiceScopeFactory serviceScopes) : base(infrastructure)
        => _serviceScopes = serviceScopes;

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<SiteKitCreateOperationalRecordSettings>();
        if (context.ExecutionContext is null)
            return ActionResult.Failed(new UnauthorizedAccessException("An automation service account is required."), StepRunErrorCategory.Authentication);
        if (!SiteKitProjectValidator.IsValidId(settings.ProjectId) || !SiteKitProjectValidator.IsValidId(settings.EntityAlias))
            return ActionResult.Failed(new ArgumentException("Project ID or entity alias is invalid."), StepRunErrorCategory.Validation);
        if (string.IsNullOrWhiteSpace(settings.IdempotencyKey) || settings.IdempotencyKey.Length > 256)
            return ActionResult.Failed(new ArgumentException("A stable idempotency key of 1 to 256 characters is required."), StepRunErrorCategory.Validation);
        if (settings.DataJson.Length > MaximumJsonLength)
            return ActionResult.Failed(new ArgumentException("Record JSON exceeds the 1 MiB limit."), StepRunErrorCategory.Validation);

        try
        {
            using var dataDocument = JsonDocument.Parse(settings.DataJson, new JsonDocumentOptions { MaxDepth = 16 });
            var data = dataDocument.RootElement;
            if (data.ValueKind != JsonValueKind.Object)
                return ActionResult.Failed(new ArgumentException("Record data must be a JSON object."), StepRunErrorCategory.Validation);
            if (data.TryGetProperty("id", out _))
                return ActionResult.Failed(new ArgumentException("Record IDs are assigned by SiteKit and cannot be supplied in record data."), StepRunErrorCategory.Validation);

            await using var scope = _serviceScopes.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var entities = services.GetRequiredService<ISiteKitEntityRegistry>();
            var records = services.GetRequiredService<ISiteKitRecordStore>();
            var permissions = services.GetRequiredService<ISiteKitPermissionRegistry>();
            var users = services.GetRequiredService<Umbraco.Cms.Core.Services.IUserService>();
            var events = services.GetRequiredService<IEventAggregator>();

            var serviceAccount = await users.GetAsync(context.ExecutionContext.ServiceAccountKey);
            var actor = serviceAccount?.Email;
            if (string.IsNullOrWhiteSpace(actor))
                return ActionResult.Failed(new UnauthorizedAccessException("The automation service account could not be resolved."), StepRunErrorCategory.Authentication);
            var grant = await permissions.GetAsync(settings.ProjectId, settings.EntityAlias, actor, cancellationToken);
            if (grant?.Operations.Contains("create", StringComparer.Ordinal) != true && grant?.Operations.Contains("manage", StringComparer.Ordinal) != true)
                return ActionResult.Failed(new UnauthorizedAccessException("The automation service account has no SiteKit create grant for this entity."), StepRunErrorCategory.Authentication);

            var entity = await entities.GetAsync(settings.ProjectId, settings.EntityAlias, cancellationToken);
            if (entity is null)
                return ActionResult.Failed(new InvalidOperationException("SiteKit entity was not found."), StepRunErrorCategory.Validation);
            var validationErrors = SiteKitEntityValidator.ValidateRecord(entity, data).ToList();
            foreach (var field in entity.Fields.Where(field => field.Type is "relationship" or "relationships" && data.TryGetProperty(field.Alias, out _)))
            {
                var targetAlias = field.RelatedEntity!;
                if (await entities.GetAsync(settings.ProjectId, targetAlias, cancellationToken) is null)
                {
                    validationErrors.Add($"Related entity '{targetAlias}' is not defined.");
                    continue;
                }

                var ids = field.Type == "relationships"
                    ? data.GetProperty(field.Alias).EnumerateArray().Select(value => value.GetString()!).ToArray()
                    : [data.GetProperty(field.Alias).GetString()!];
                foreach (var id in ids)
                    if (await records.GetAsync(settings.ProjectId, targetAlias, id, cancellationToken) is null)
                        validationErrors.Add($"Relationship '{field.Alias}' references a missing {targetAlias} record.");
            }
            if (validationErrors.Count != 0)
                return ActionResult.Failed(new ArgumentException(string.Join(" ", validationErrors)), StepRunErrorCategory.Validation);

            var idempotencyMaterial = $"sitekit-create:{settings.ProjectId}:{settings.EntityAlias}:{settings.IdempotencyKey}";
            var idempotencyHash = SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyMaterial));
            var recordId = new Guid(idempotencyHash.AsSpan(0, 16)).ToString("N");
            var previous = await records.GetAsync(settings.ProjectId, settings.EntityAlias, recordId, cancellationToken);
            if (previous is not null)
                return Success(new SiteKitCreateOperationalRecordOutput
                { ProjectId = settings.ProjectId, EntityAlias = settings.EntityAlias, RecordId = previous.Id, AlreadyCreated = true });

            var created = await records.SaveAsync(entity, data, actor, recordId, cancellationToken);
            events.Publish(new SiteKitOperationalRecordChangedNotification(settings.ProjectId, settings.EntityAlias, created.Id, "created", created.UpdatedAt));
            return Success(new SiteKitCreateOperationalRecordOutput
            { ProjectId = settings.ProjectId, EntityAlias = settings.EntityAlias, RecordId = created.Id, AlreadyCreated = false });
        }
        catch (JsonException exception) { return ActionResult.Failed(exception, StepRunErrorCategory.Validation); }
        catch (UnauthorizedAccessException exception) { return ActionResult.Failed(exception, StepRunErrorCategory.Authentication); }
        catch (ArgumentException exception) { return ActionResult.Failed(exception, StepRunErrorCategory.Validation); }
        catch (InvalidOperationException exception) { return ActionResult.Failed(exception, StepRunErrorCategory.Validation); }
    }
}
