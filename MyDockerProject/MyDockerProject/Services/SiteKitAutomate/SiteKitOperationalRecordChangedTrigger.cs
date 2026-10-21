using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Notifications;

namespace MyDockerProject.Services.SiteKitAutomate;

/// <summary>Published after a persisted SiteKit operational record is changed or removed.</summary>
public sealed record SiteKitOperationalRecordChangedNotification(
    string ProjectId,
    string EntityAlias,
    string RecordId,
    string Operation,
    DateTimeOffset ChangedAt) : INotification;

public sealed class SiteKitOperationalRecordChangedSettings
{
    public string ProjectId { get; set; } = string.Empty;
    public string EntityAlias { get; set; } = string.Empty;
    public string Operation { get; set; } = "any";
}

public sealed class SiteKitOperationalRecordChangedOutput
{
    public string ProjectId { get; set; } = string.Empty;
    public string EntityAlias { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
}

[Trigger("sitekit.operationalRecordChanged", "SiteKit Operational Record Changed", Description = "Runs after a permitted SiteKit record create, update or delete operation.", Group = "SiteKit", Icon = "icon-box")]
public sealed class SiteKitOperationalRecordChangedTrigger : NotificationTriggerBase<SiteKitOperationalRecordChangedSettings, SiteKitOperationalRecordChangedOutput, SiteKitOperationalRecordChangedNotification>
{
    public SiteKitOperationalRecordChangedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure) { }

    public override IEnumerable<TriggerEvent> MapEvent(SiteKitOperationalRecordChangedNotification notification)
    {
        yield return new TriggerEvent<SiteKitOperationalRecordChangedOutput>
        {
            TriggerAlias = "sitekit.operationalRecordChanged",
            InitiatorType = "SiteKit",
            InitiatorId = notification.RecordId,
            IdempotencyKey = $"sitekit-record:{notification.ProjectId}:{notification.EntityAlias}:{notification.RecordId}:{notification.ChangedAt.UtcTicks}",
            Output = new SiteKitOperationalRecordChangedOutput
            {
                ProjectId = notification.ProjectId,
                EntityAlias = notification.EntityAlias,
                RecordId = notification.RecordId,
                Operation = notification.Operation,
                ChangedAt = notification.ChangedAt
            }
        };
    }

    protected override bool CanHandle(SiteKitOperationalRecordChangedOutput output, SiteKitOperationalRecordChangedSettings? settings)
        => settings is not null
            && string.Equals(settings.ProjectId, output.ProjectId, StringComparison.Ordinal)
            && string.Equals(settings.EntityAlias, output.EntityAlias, StringComparison.Ordinal)
            && (settings.Operation is "any" or "created" or "updated" or "deleted")
            && (settings.Operation == "any" || string.Equals(settings.Operation, output.Operation, StringComparison.Ordinal));
}
