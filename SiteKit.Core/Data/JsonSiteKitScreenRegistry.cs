using System.Text.Json;
using SiteKit.Core.Projects;

namespace SiteKit.Core.Data;

public sealed class JsonSiteKitScreenRegistry : ISiteKitScreenRegistry
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitScreenRegistry(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<SiteKitApplicationScreenDefinition>> ListAsync(string projectId, CancellationToken cancellationToken = default)
    {
        ValidateProject(projectId);
        var directory = Path.Combine(_root, projectId);
        if (!Directory.Exists(directory)) return [];
        var screens = new List<SiteKitApplicationScreenDefinition>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            await using var stream = File.OpenRead(path);
            var screen = await JsonSerializer.DeserializeAsync<SiteKitApplicationScreenDefinition>(stream, Options, cancellationToken);
            if (screen is not null && SiteKitScreenValidator.Validate(screen).Count == 0) screens.Add(screen);
        }
        return screens.OrderBy(screen => screen.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<SiteKitApplicationScreenDefinition?> GetAsync(string projectId, string alias, CancellationToken cancellationToken = default)
    {
        ValidateProject(projectId); ValidateAlias(alias);
        var path = Path.Combine(_root, projectId, alias + ".json");
        if (!File.Exists(path)) return null;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<SiteKitApplicationScreenDefinition>(stream, Options, cancellationToken);
    }

    public async Task<SiteKitApplicationScreenDefinition> SaveAsync(SiteKitApplicationScreenDefinition screen, CancellationToken cancellationToken = default)
    {
        var errors = SiteKitScreenValidator.Validate(screen);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.Combine(_root, screen.ProjectId);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, screen.Alias + ".json");
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    await JsonSerializer.SerializeAsync(stream, screen with { UpdatedAt = DateTimeOffset.UtcNow }, Options, cancellationToken);
                File.Move(temp, path, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return await GetAsync(screen.ProjectId, screen.Alias, cancellationToken) ?? throw new IOException("Saved screen could not be read.");
        }
        finally { _gate.Release(); }
    }

    private static void ValidateProject(string id) { if (!SiteKitProjectValidator.IsValidId(id)) throw new ArgumentException("Project id is invalid.", nameof(id)); }
    private static void ValidateAlias(string alias) { if (!SiteKitProjectValidator.IsValidId(alias)) throw new ArgumentException("Screen alias is invalid.", nameof(alias)); }
}
