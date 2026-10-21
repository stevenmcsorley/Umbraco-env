namespace SiteKit.Core.Components;

public interface ISiteKitPlacementRegistry
{
    Task<IReadOnlyList<SiteKitComponentPlacement>> ListAsync(string projectId, CancellationToken cancellationToken = default);
    Task<SiteKitComponentPlacement?> GetAsync(string projectId, string placementId, CancellationToken cancellationToken = default);
    Task<SiteKitComponentPlacement> SaveAsync(string projectId, SiteKitComponentPlacement placement, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string projectId, string placementId, CancellationToken cancellationToken = default);
}
