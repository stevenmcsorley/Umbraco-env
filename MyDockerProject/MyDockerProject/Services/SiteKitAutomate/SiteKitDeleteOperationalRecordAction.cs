using SiteKit.Core.Data;
using SiteKit.Core.Projects;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Settings;
using Umbraco.Cms.Core.Events;

namespace MyDockerProject.Services.SiteKitAutomate;

public sealed class SiteKitDeleteOperationalRecordSettings
{
    [Field(Label = "Project ID", Description = "SiteKit project that owns the record.", SupportsBindings = true)]
    public string ProjectId { get; set; } = string.Empty;

    [Field(Label = "Entity alias", Description = "SiteKit entity containing the record.", SupportsBindings = true)]
    public string EntityAlias { get; set; } = string.Empty;

    [Field(Label = "Record ID", Description = "UUID of the record to delete.", SupportsBindings = true)]
    public string RecordId { get; set; } = string.Empty;
}

public sealed class SiteKitDeleteOperationalRecordOutput
{
    public string ProjectId { get; set; } = string.Empty;
    public string EntityAlias { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public bool AlreadyDeleted { get; set; }
}

[Action("sitekit.deleteOperationalRecord", "Delete SiteKit Operational Record",
    Description = "Delete a permission-checked SiteKit business record and publish its change event. Replays are safe.",
    Group = "SiteKit", Icon = "icon-trash")]
public sealed class SiteKitDeleteOperationalRecordAction : ActionBase<SiteKitDeleteOperationalRecordSettings, SiteKitDeleteOperationalRecordOutput>
{
    private readonly IServiceScopeFactory _serviceScopes;

    public SiteKitDeleteOperationalRecordAction(ActionInfrastructure infrastructure, IServiceScopeFactory serviceScopes) : base(infrastructure)
        => _serviceScopes = serviceScopes;

    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<SiteKitDeleteOperationalRecordSettings>();
        if (context.ExecutionContext is null)
            return ActionResult.Failed(new UnauthorizedAccessException("An automation service account is required."), StepRunErrorCategory.Authentication);
        if (!SiteKitProjectValidator.IsValidId(settings.ProjectId) || !SiteKitProjectValidator.IsValidId(settings.EntityAlias) || !Guid.TryParse(settings.RecordId, out _))
            return ActionResult.Failed(new ArgumentException("Project ID, entity alias or record UUID is invalid."), StepRunErrorCategory.Validation);

        try
        {
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
            if (grant?.Operations.Contains("delete", StringComparer.Ordinal) != true && grant?.Operations.Contains("manage", StringComparer.Ordinal) != true)
                return ActionResult.Failed(new UnauthorizedAccessException("The automation service account has no SiteKit delete grant for this entity."), StepRunErrorCategory.Authentication);

            if (await entities.GetAsync(settings.ProjectId, settings.EntityAlias, cancellationToken) is null)
                return ActionResult.Failed(new InvalidOperationException("SiteKit entity was not found."), StepRunErrorCategory.Validation);

            var record = await records.GetAsync(settings.ProjectId, settings.EntityAlias, settings.RecordId, cancellationToken);
            if (record is null)
                return Success(new SiteKitDeleteOperationalRecordOutput
                { ProjectId = settings.ProjectId, EntityAlias = settings.EntityAlias, RecordId = Guid.Parse(settings.RecordId).ToString("N"), AlreadyDeleted = true });

            var deleted = await records.DeleteAsync(settings.ProjectId, settings.EntityAlias, record.Id, actor,
                await entities.ListAsync(settings.ProjectId, cancellationToken), cancellationToken);
            if (deleted)
                events.Publish(new SiteKitOperationalRecordChangedNotification(settings.ProjectId, settings.EntityAlias, record.Id, "deleted", DateTimeOffset.UtcNow));

            return Success(new SiteKitDeleteOperationalRecordOutput
            { ProjectId = settings.ProjectId, EntityAlias = settings.EntityAlias, RecordId = record.Id, AlreadyDeleted = !deleted });
        }
        catch (UnauthorizedAccessException exception) { return ActionResult.Failed(exception, StepRunErrorCategory.Authentication); }
        catch (ArgumentException exception) { return ActionResult.Failed(exception, StepRunErrorCategory.Validation); }
        catch (InvalidOperationException exception) { return ActionResult.Failed(exception, StepRunErrorCategory.Validation); }
    }
}
