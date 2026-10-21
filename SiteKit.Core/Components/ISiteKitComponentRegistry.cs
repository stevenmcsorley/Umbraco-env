namespace SiteKit.Core.Components;

public interface ISiteKitComponentRegistry
{
    Task<IReadOnlyList<SiteKitComponentDefinition>> ListAsync(CancellationToken cancellationToken = default);
    Task<SiteKitComponentDefinition?> GetAsync(string alias, string? version = null, CancellationToken cancellationToken = default);
    Task<SiteKitComponentDefinition> SaveAsync(SiteKitComponentDefinition definition, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string alias, string version, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAsync(string alias, string version, CancellationToken cancellationToken = default);
    Task<SiteKitComponentDefinition> ImportAsync(Stream package, CancellationToken cancellationToken = default);
}
