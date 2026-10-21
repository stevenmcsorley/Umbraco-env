using System.Text.Json;

namespace SiteKit.Core.Data;

/// <summary>Local development record store. Production providers should use SQL-backed stores and the same entity contracts.</summary>
public sealed class JsonSiteKitRecordStore : ISiteKitRecordStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitRecordStore(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<SiteKitEntityRecord>> ListAsync(string projectId, string entityAlias, CancellationToken cancellationToken = default)
    {
        ValidateIds(projectId, entityAlias);
        var directory = GetDirectory(projectId, entityAlias);
        if (!Directory.Exists(directory)) return [];
        var records = new List<SiteKitEntityRecord>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            await using var stream = File.OpenRead(path);
            var record = await JsonSerializer.DeserializeAsync<SiteKitEntityRecord>(stream, Options, cancellationToken);
            if (record is not null) records.Add(record);
        }
        return records.OrderByDescending(x => x.UpdatedAt).ToArray();
    }

    public async Task<SiteKitEntityRecord?> GetAsync(string projectId, string entityAlias, string id, CancellationToken cancellationToken = default)
    {
        ValidateIds(projectId, entityAlias);
        if (!Guid.TryParse(id, out var recordId)) throw new ArgumentException("Record id must be a UUID.", nameof(id));
        var path = Path.Combine(GetDirectory(projectId, entityAlias), recordId.ToString("N") + ".json");
        if (!File.Exists(path)) return null;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<SiteKitEntityRecord>(stream, Options, cancellationToken);
    }

    public async Task<SiteKitEntityRecord> SaveAsync(SiteKitEntityDefinition definition, JsonElement data, string actor, string? id = null, CancellationToken cancellationToken = default)
    {
        var errors = SiteKitEntityValidator.ValidateRecord(definition, data);
        if (errors.Count != 0) throw new ArgumentException(string.Join(" ", errors));
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 320) throw new ArgumentException("An authenticated actor is required.", nameof(actor));
        var key = Guid.TryParse(id, out var parsed) ? parsed : Guid.NewGuid();
        if (id is not null && !Guid.TryParse(id, out _)) throw new ArgumentException("Record id must be a UUID.", nameof(id));
        if (data.TryGetProperty("id", out var given) && Guid.TryParse(given.GetString(), out var givenId) && givenId != key) throw new ArgumentException("Body id does not match the route id.", nameof(data));
        var normalized = JsonSerializer.SerializeToElement(data.EnumerateObject().Where(p => p.Name != "id").ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal));
        var path = Path.Combine(GetDirectory(definition.ProjectId, definition.Alias), key.ToString("N") + ".json");
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var existing = await GetAsync(definition.ProjectId, definition.Alias, key.ToString("N"), cancellationToken);
            foreach (var field in definition.Fields.Where(f => f.Unique && normalized.TryGetProperty(f.Alias, out _)))
            {
                var value = normalized.GetProperty(field.Alias);
                foreach (var other in await ListAsync(definition.ProjectId, definition.Alias, cancellationToken))
                    if (other.Id != key.ToString("N") && other.Data.TryGetProperty(field.Alias, out var otherValue) && JsonElement.DeepEquals(value, otherValue))
                        throw new InvalidOperationException($"The value of unique field '{field.Alias}' is already in use.");
            }
            var now = DateTimeOffset.UtcNow;
            var record = new SiteKitEntityRecord
            {
                Id = key.ToString("N"), ProjectId = definition.ProjectId, EntityAlias = definition.Alias,
                SchemaVersion = definition.Version, Data = normalized, CreatedBy = existing?.CreatedBy ?? actor,
                UpdatedBy = actor, CreatedAt = existing?.CreatedAt ?? now, UpdatedAt = now
            };
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    await JsonSerializer.SerializeAsync(stream, record, Options, cancellationToken);
                File.Move(temp, path, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return record;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DeleteAsync(string projectId, string entityAlias, string id, string actor, IReadOnlyList<SiteKitEntityDefinition> entityDefinitions, CancellationToken cancellationToken = default)
    {
        ValidateIds(projectId, entityAlias);
        if (!Guid.TryParse(id, out var recordId)) throw new ArgumentException("Record id must be a UUID.", nameof(id));
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = Path.Combine(GetDirectory(projectId, entityAlias), recordId.ToString("N") + ".json");
            if (!File.Exists(path)) return false;
            foreach (var source in entityDefinitions.Where(definition => definition.ProjectId == projectId))
            foreach (var field in source.Fields.Where(field => field.Type is "relationship" or "relationships" && field.RelatedEntity == entityAlias))
            foreach (var sourceRecord in await ListAsync(projectId, source.Alias, cancellationToken))
            {
                if (!sourceRecord.Data.TryGetProperty(field.Alias, out var value)) continue;
                var references = field.Type == "relationships" && value.ValueKind == JsonValueKind.Array
                    ? value.EnumerateArray().Any(item => IsSameId(item, recordId))
                    : IsSameId(value, recordId);
                if (references) throw new InvalidOperationException($"Cannot delete {entityAlias} record '{recordId:N}' because {source.Alias} record '{sourceRecord.Id}' references it through '{field.Alias}'.");
            }
            File.Delete(path);
            return true;
        }
        finally { _gate.Release(); }
    }

    private static bool IsSameId(JsonElement value, Guid id) => value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var referencedId) && referencedId == id;

    private string GetDirectory(string projectId, string entityAlias) => Path.Combine(_root, projectId, entityAlias);
    private static void ValidateIds(string projectId, string alias)
    {
        if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(projectId) || !System.Text.RegularExpressions.Regex.IsMatch(alias, "^[a-z][a-z0-9-]{1,63}$"))
            throw new ArgumentException("Project or entity identifier is invalid.");
    }
}
