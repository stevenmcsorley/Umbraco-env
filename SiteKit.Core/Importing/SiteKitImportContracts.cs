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
    public string? SourceUrl { get; init; }
    public bool HasHeader { get; init; } = true;
    public string Delimiter { get; init; } = ",";
}

public sealed record SiteKitImportRowError(int Row, string Error);

public sealed record SiteKitImportPreview(int RowsRead, int ValidRows, IReadOnlyList<SiteKitImportRowError> Errors, IReadOnlyList<System.Text.Json.JsonElement> Samples);

public sealed record SiteKitImportResult(int RowsRead, int Created, int Updated, IReadOnlyList<SiteKitImportRowError> Errors);

public sealed record SiteKitImportRun
{
    public int SchemaVersion { get; init; } = 1;
    public required string Id { get; init; }
    public required string ProjectId { get; init; }
    public required string MappingAlias { get; init; }
    public required string EntityAlias { get; init; }
    public required string Actor { get; init; }
    public required string Mode { get; init; }
    public required string Status { get; init; }
    public required string ContentSha256 { get; init; }
    public int RowsRead { get; init; }
    public int Created { get; init; }
    public int Updated { get; init; }
    public int ErrorCount { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; init; }
}

public interface ISiteKitImportMappingRegistry
{
    Task<IReadOnlyList<SiteKitImportMapping>> ListAsync(string projectId, CancellationToken cancellationToken = default);
    Task<SiteKitImportMapping?> GetAsync(string projectId, string alias, CancellationToken cancellationToken = default);
    Task<SiteKitImportMapping> SaveAsync(SiteKitImportMapping mapping, CancellationToken cancellationToken = default);
}

public interface ISiteKitImportRunRegistry
{
    Task<IReadOnlyList<SiteKitImportRun>> ListAsync(string projectId, string mappingAlias, CancellationToken cancellationToken = default);
    Task<SiteKitImportRun> SaveAsync(SiteKitImportRun run, CancellationToken cancellationToken = default);
}
