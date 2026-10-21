using System.Text.Json;
using System.Text.RegularExpressions;

namespace SiteKit.Core.Data;

/// <summary>Versioned local entity definitions. Runtime records can move to a provider-specific database without changing the contract.</summary>
public sealed class JsonSiteKitEntityRegistry : ISiteKitEntityRegistry
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitEntityRegistry(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<SiteKitEntityDefinition>> ListAsync(string projectId, CancellationToken cancellationToken = default)
    {
        ValidateProject(projectId);
        var directory = Path.Combine(_root, projectId);
        if (!Directory.Exists(directory)) return [];
        var values = new List<SiteKitEntityDefinition>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            await using var stream = File.OpenRead(path);
            var entity = await JsonSerializer.DeserializeAsync<SiteKitEntityDefinition>(stream, Options, cancellationToken);
            if (entity is not null && SiteKitEntityValidator.Validate(entity).Count == 0) values.Add(entity);
        }
        return values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<SiteKitEntityDefinition?> GetAsync(string projectId, string alias, CancellationToken cancellationToken = default)
    {
        ValidateProject(projectId); ValidateAlias(alias);
        var file = Path.Combine(_root, projectId, alias + ".json");
        if (!File.Exists(file)) return null;
        await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<SiteKitEntityDefinition>(stream, Options, cancellationToken);
    }

    public async Task<SiteKitEntityDefinition> SaveAsync(SiteKitEntityDefinition definition, CancellationToken cancellationToken = default)
    {
        var errors = SiteKitEntityValidator.Validate(definition);
        if (errors.Count != 0) throw new ArgumentException(string.Join(" ", errors));
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.Combine(_root, definition.ProjectId);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, definition.Alias + ".json");
            var current = File.Exists(path) ? await GetAsync(definition.ProjectId, definition.Alias, cancellationToken) : null;
            if (current is not null && definition.Version < current.Version) throw new InvalidOperationException("Schema versions cannot move backwards.");
            if (current is not null)
            {
                if (definition.Version > current.Version + 1) throw new InvalidOperationException("Schema versions must be upgraded one version at a time.");
                var nextFields = definition.Fields.ToDictionary(field => field.Alias, StringComparer.Ordinal);
                var removedOrChanged = current.Fields.Any(old => !nextFields.TryGetValue(old.Alias, out var updated) ||
                    updated.Type != old.Type || updated.Required != old.Required || updated.Unique != old.Unique ||
                    updated.RelatedEntity != old.RelatedEntity || updated.MaximumLength != old.MaximumLength);
                var addedRequired = definition.Fields.Any(field => current.Fields.All(old => old.Alias != field.Alias) && field.Required);
                var schemaChanged = removedOrChanged || addedRequired || current.Fields.Length != definition.Fields.Length ||
                    current.Fields.Any(old => !nextFields.TryGetValue(old.Alias, out var updated) || updated.Name != old.Name);
                if (removedOrChanged) throw new InvalidOperationException("This version cannot remove fields or change field types, constraints or relationships; existing records are preserved.");
                if (addedRequired) throw new InvalidOperationException("New required fields need a reviewed data migration before they can be added.");
                if (schemaChanged && definition.Version != current.Version + 1) throw new InvalidOperationException("Schema changes must increment the version by exactly one.");
            }
            var saved = definition with { UpdatedAt = DateTimeOffset.UtcNow };
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    await JsonSerializer.SerializeAsync(output, saved, Options, cancellationToken);
                File.Move(temp, path, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return saved;
        }
        finally { _gate.Release(); }
    }

    private static void ValidateProject(string id) { if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(id)) throw new ArgumentException("Project id is invalid.", nameof(id)); }
    private static void ValidateAlias(string alias) { if (!Regex.IsMatch(alias, "^[a-z][a-z0-9-]{1,63}$")) throw new ArgumentException("Entity alias is invalid.", nameof(alias)); }
}
