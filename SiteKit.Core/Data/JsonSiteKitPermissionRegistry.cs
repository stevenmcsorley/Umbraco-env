using System.Text.Json;

namespace SiteKit.Core.Data;

/// <summary>Explicit project/entity grants keyed by the existing authenticated account identifier.</summary>
public sealed class JsonSiteKitPermissionRegistry : ISiteKitPermissionRegistry
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitPermissionRegistry(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<SiteKitEntityPermissionGrant>> ListAsync(string projectId, string entityAlias, CancellationToken cancellationToken = default)
    {
        Validate(projectId, entityAlias);
        var directory = Path.Combine(_root, projectId, entityAlias);
        if (!Directory.Exists(directory)) return [];
        var grants = new List<SiteKitEntityPermissionGrant>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            await using var stream = File.OpenRead(file);
            var grant = await JsonSerializer.DeserializeAsync<SiteKitEntityPermissionGrant>(stream, Options, cancellationToken);
            if (grant is not null) grants.Add(grant);
        }
        return grants;
    }

    public async Task<SiteKitEntityPermissionGrant?> GetAsync(string projectId, string entityAlias, string principal, CancellationToken cancellationToken = default)
    {
        Validate(projectId, entityAlias);
        var file = GetFile(projectId, entityAlias, principal);
        if (!File.Exists(file)) return null;
        await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<SiteKitEntityPermissionGrant>(stream, Options, cancellationToken);
    }

    public async Task<SiteKitEntityPermissionGrant> SaveAsync(SiteKitEntityPermissionGrant grant, CancellationToken cancellationToken = default)
    {
        Validate(grant.ProjectId, grant.EntityAlias);
        if (string.IsNullOrWhiteSpace(grant.Principal) || grant.Principal.Length > 320 ||
            (!SiteKitPermissionEvaluator.TryGetRoleAlias(grant.Principal, out _) && grant.Principal.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            throw new ArgumentException("Permission principal is invalid.", nameof(grant));
        string[] supported = ["read", "create", "update", "delete", "import", "manage"];
        if (grant.Operations is null || grant.Operations.Distinct(StringComparer.Ordinal).Count() != grant.Operations.Length || grant.Operations.Any(x => !supported.Contains(x, StringComparer.Ordinal)))
            throw new ArgumentException("Grant operations contain an unsupported or duplicate operation.", nameof(grant));
        var file = GetFile(grant.ProjectId, grant.EntityAlias, grant.Principal);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    await JsonSerializer.SerializeAsync(stream, grant with { Operations = grant.Operations.Distinct(StringComparer.Ordinal).ToArray() }, Options, cancellationToken);
                File.Move(temp, file, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return grant;
        }
        finally { _gate.Release(); }
    }

    private string GetFile(string projectId, string alias, string principal)
    {
        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(principal.ToLowerInvariant())));
        return Path.Combine(_root, projectId, alias, digest + ".json");
    }

    private static void Validate(string projectId, string alias)
    {
        if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(projectId) || !System.Text.RegularExpressions.Regex.IsMatch(alias, "^[a-z][a-z0-9-]{1,63}$"))
            throw new ArgumentException("Project or entity identifier is invalid.");
    }
}
