using System.Text.Json;

namespace SiteKit.Core.Data;

/// <summary>Project-scoped role membership for resolving entity permission grants.</summary>
public sealed class JsonSiteKitProjectRoleRegistry : ISiteKitProjectRoleRegistry
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitProjectRoleRegistry(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<SiteKitProjectRole>> ListAsync(string projectId, CancellationToken cancellationToken = default)
    {
        ValidateProject(projectId);
        var directory = Path.Combine(_root, projectId);
        if (!Directory.Exists(directory)) return [];
        var roles = new List<SiteKitProjectRole>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            await using var stream = File.OpenRead(file);
            var role = await JsonSerializer.DeserializeAsync<SiteKitProjectRole>(stream, Options, cancellationToken);
            if (role is not null) roles.Add(role);
        }
        return roles.OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<SiteKitProjectRole?> GetAsync(string projectId, string alias, CancellationToken cancellationToken = default)
    {
        ValidateIds(projectId, alias);
        var path = GetPath(projectId, alias);
        if (!File.Exists(path)) return null;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<SiteKitProjectRole>(stream, Options, cancellationToken);
    }

    public async Task<SiteKitProjectRole> SaveAsync(SiteKitProjectRole role, CancellationToken cancellationToken = default)
    {
        var normalized = role with { Members = role.Members?.Select(member => member.Trim()).ToArray() ?? [] };
        var errors = SiteKitProjectRoleValidator.Validate(normalized);
        if (errors.Count != 0) throw new ArgumentException(string.Join(" ", errors), nameof(role));
        var path = GetPath(normalized.ProjectId, normalized.Alias);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var saved = normalized with { Members = normalized.Members.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(), UpdatedAt = DateTimeOffset.UtcNow };
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    await JsonSerializer.SerializeAsync(stream, saved, Options, cancellationToken);
                File.Move(temp, path, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return saved;
        }
        finally { _gate.Release(); }
    }

    private string GetPath(string projectId, string alias) => Path.Combine(_root, projectId, alias + ".json");
    private static void ValidateProject(string projectId)
    {
        if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(projectId)) throw new ArgumentException("Project identifier is invalid.");
    }
    private static void ValidateIds(string projectId, string alias)
    {
        ValidateProject(projectId);
        if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(alias)) throw new ArgumentException("Role alias is invalid.");
    }
}
