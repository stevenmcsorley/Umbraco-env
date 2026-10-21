namespace SiteKit.Core.Importing;

public sealed record SiteKitImportMapping
{
    public int SchemaVersion { get; init; } = 1;
    public required string ProjectId { get; init; }
    public required string Alias { get; init; }
    public required string Name { get; init; }
    public required string EntityAlias { get; init; }
    public required Dictionary<string, string> Fields { get; init; }
    public required string[] MatchFields { get; init; }
    public string Format { get; init; } = "json";
    public bool HasHeader { get; init; } = true;
    public string Delimiter { get; init; } = ",";
}

public sealed record SiteKitImportRowError(int Row, string Error);

public sealed record SiteKitImportPreview(int RowsRead, int ValidRows, IReadOnlyList<SiteKitImportRowError> Errors, IReadOnlyList<System.Text.Json.JsonElement> Samples);

public sealed record SiteKitImportResult(int RowsRead, int Created, int Updated, IReadOnlyList<SiteKitImportRowError> Errors);

public interface ISiteKitImportMappingRegistry
{
    Task<IReadOnlyList<SiteKitImportMapping>> ListAsync(string projectId, CancellationToken cancellationToken = default);
    Task<SiteKitImportMapping?> GetAsync(string projectId, string alias, CancellationToken cancellationToken = default);
    Task<SiteKitImportMapping> SaveAsync(SiteKitImportMapping mapping, CancellationToken cancellationToken = default);
}
