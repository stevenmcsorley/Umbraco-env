using System.Text.Json;
using SiteKit.Core.Projects;

namespace SiteKit.Core.Importing;

/// <summary>Stores bounded, value-free metadata about import and preview executions.</summary>
public sealed class JsonSiteKitImportRunRegistry : ISiteKitImportRunRegistry
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitImportRunRegistry(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<SiteKitImportRun>> ListAsync(string projectId, string mappingAlias, CancellationToken cancellationToken = default)
    {
        ValidateIds(projectId, mappingAlias);
        var directory = Path.Combine(_root, projectId, mappingAlias);
        if (!Directory.Exists(directory)) return [];
        var runs = new List<SiteKitImportRun>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var stream = File.OpenRead(path);
                var run = await JsonSerializer.DeserializeAsync<SiteKitImportRun>(stream, Options, cancellationToken);
                if (run is not null && run.ProjectId == projectId && run.MappingAlias == mappingAlias && IsValid(run)) runs.Add(run);
            }
            catch (JsonException) { }
            catch (IOException) { }
        }
        return runs.OrderByDescending(run => run.StartedAt).Take(100).ToArray();
    }

    public async Task<SiteKitImportRun> SaveAsync(SiteKitImportRun run, CancellationToken cancellationToken = default)
    {
        if (!IsValid(run)) throw new ArgumentException("Import run metadata is invalid.", nameof(run));
        ValidateIds(run.ProjectId, run.MappingAlias);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.Combine(_root, run.ProjectId, run.MappingAlias);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, run.Id + ".json");
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    await JsonSerializer.SerializeAsync(stream, run, Options, cancellationToken);
                File.Move(temporary, path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return run;
        }
        finally { _gate.Release(); }
    }

    private static bool IsValid(SiteKitImportRun run)
        => run.SchemaVersion == 1 && Guid.TryParse(run.Id, out _) &&
           SiteKitProjectValidator.IsValidId(run.ProjectId) && SiteKitProjectValidator.IsValidId(run.MappingAlias) &&
           SiteKitProjectValidator.IsValidId(run.EntityAlias) && !string.IsNullOrWhiteSpace(run.Actor) && run.Actor.Length <= 320 &&
           run.Mode is "preview" or "import" && run.Status is "complete" or "partial" or "failed" &&
           System.Text.RegularExpressions.Regex.IsMatch(run.ContentSha256 ?? string.Empty, "^[a-fA-F0-9]{64}$") &&
           run.RowsRead >= 0 && run.Created >= 0 && run.Updated >= 0 && run.ErrorCount >= 0 && run.FinishedAt >= run.StartedAt;

    private static void ValidateIds(string projectId, string mappingAlias)
    {
        if (!SiteKitProjectValidator.IsValidId(projectId) || !SiteKitProjectValidator.IsValidId(mappingAlias))
            throw new ArgumentException("Project or mapping identifier is invalid.");
    }
}
