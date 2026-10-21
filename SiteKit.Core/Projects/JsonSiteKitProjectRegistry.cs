using System.Text.Json;
using SiteKit.Core.Components;

namespace SiteKit.Core.Projects;

/// <summary>Persists project manifests separately from CMS content and component definitions.</summary>
public sealed class JsonSiteKitProjectRegistry : ISiteKitProjectRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitProjectRegistry(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<SiteKitProjectManifest>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_root)) return [];
        var projects = new List<SiteKitProjectManifest>();
        foreach (var file in Directory.EnumerateFiles(_root, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
                var manifest = await JsonSerializer.DeserializeAsync<SiteKitProjectManifest>(stream, JsonOptions, cancellationToken);
                if (manifest is not null && SiteKitProjectValidator.Validate(manifest).Count == 0) projects.Add(manifest);
            }
            catch (JsonException) { }
            catch (IOException) { }
        }
        return projects.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<SiteKitProjectManifest?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        var file = Path.Combine(_root, id + ".json");
        if (!File.Exists(file)) return null;
        await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<SiteKitProjectManifest>(stream, JsonOptions, cancellationToken);
    }

    public async Task<SiteKitProjectManifest> SaveAsync(SiteKitProjectManifest manifest, CancellationToken cancellationToken = default)
    {
        var errors = SiteKitProjectValidator.Validate(manifest);
        if (errors.Count != 0) throw new ArgumentException(string.Join(" ", errors), nameof(manifest));
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(_root);
            var destination = Path.Combine(_root, manifest.Id + ".json");
            var temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var saved = manifest with { UpdatedAt = DateTimeOffset.UtcNow };
            try
            {
                await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    await JsonSerializer.SerializeAsync(stream, saved, JsonOptions, cancellationToken);
                File.Move(temp, destination, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return saved;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var file = Path.Combine(_root, id + ".json");
            if (!File.Exists(file)) return false;
            File.Delete(file);
            return true;
        }
        finally { _gate.Release(); }
    }

    private static void ValidateId(string id)
    {
        if (!SiteKitProjectValidator.IsValidId(id))
            throw new ArgumentException("Project id is invalid.", nameof(id));
    }
}
