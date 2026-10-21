using System.Text.Json;

namespace SiteKit.Core.Data;

public interface ISiteKitEntityRegistry
{
    Task<IReadOnlyList<SiteKitEntityDefinition>> ListAsync(string projectId, CancellationToken cancellationToken = default);
    Task<SiteKitEntityDefinition?> GetAsync(string projectId, string alias, CancellationToken cancellationToken = default);
    Task<SiteKitEntityDefinition> SaveAsync(SiteKitEntityDefinition definition, CancellationToken cancellationToken = default);
}

public interface ISiteKitRecordStore
{
    Task<IReadOnlyList<SiteKitEntityRecord>> ListAsync(string projectId, string entityAlias, CancellationToken cancellationToken = default);
    Task<SiteKitEntityRecord?> GetAsync(string projectId, string entityAlias, string id, CancellationToken cancellationToken = default);
    Task<SiteKitEntityRecord> SaveAsync(SiteKitEntityDefinition definition, JsonElement data, string actor, string? id = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string projectId, string entityAlias, string id, string actor, IReadOnlyList<SiteKitEntityDefinition> entityDefinitions, CancellationToken cancellationToken = default);
}

public interface ISiteKitScreenRegistry
{
    Task<IReadOnlyList<SiteKitApplicationScreenDefinition>> ListAsync(string projectId, CancellationToken cancellationToken = default);
    Task<SiteKitApplicationScreenDefinition?> GetAsync(string projectId, string alias, CancellationToken cancellationToken = default);
    Task<SiteKitApplicationScreenDefinition> SaveAsync(SiteKitApplicationScreenDefinition screen, CancellationToken cancellationToken = default);
}
