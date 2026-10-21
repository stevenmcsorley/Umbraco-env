using System.Text.Json;
using SiteKit.Core.Projects;

namespace SiteKit.Core.Importing;

public sealed class JsonSiteKitImportMappingRegistry : ISiteKitImportMappingRegistry
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitImportMappingRegistry(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<SiteKitImportMapping>> ListAsync(string projectId, CancellationToken cancellationToken = default)
    {
        ValidateProject(projectId);
        var directory = Path.Combine(_root, projectId);
        if (!Directory.Exists(directory)) return [];
        var result = new List<SiteKitImportMapping>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            try
            {
                await using var stream = File.OpenRead(path);
                var mapping = await JsonSerializer.DeserializeAsync<SiteKitImportMapping>(stream, Options, cancellationToken);
                if (mapping is not null && Validate(mapping).Count == 0) result.Add(mapping);
            }
            catch (JsonException) { }
            catch (IOException) { }
        }
        return result.OrderBy(mapping => mapping.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<SiteKitImportMapping?> GetAsync(string projectId, string alias, CancellationToken cancellationToken = default)
    {
        ValidateProject(projectId); ValidateAlias(alias);
        var path = Path.Combine(_root, projectId, alias + ".json");
        if (!File.Exists(path)) return null;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<SiteKitImportMapping>(stream, Options, cancellationToken);
    }

    public async Task<SiteKitImportMapping> SaveAsync(SiteKitImportMapping mapping, CancellationToken cancellationToken = default)
    {
        var errors = Validate(mapping);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.Combine(_root, mapping.ProjectId);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, mapping.Alias + ".json");
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                    await JsonSerializer.SerializeAsync(stream, mapping, Options, cancellationToken);
                File.Move(temp, path, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return await GetAsync(mapping.ProjectId, mapping.Alias, cancellationToken) ?? throw new IOException("Saved import mapping could not be read.");
        }
        finally { _gate.Release(); }
    }

    public static IReadOnlyList<string> Validate(SiteKitImportMapping mapping)
    {
        var errors = new List<string>();
        if (mapping is null) return ["mapping is required."];
        if (mapping.SchemaVersion != 1) errors.Add("mapping schemaVersion must be 1.");
        if (!SiteKitProjectValidator.IsValidId(mapping.ProjectId)) errors.Add("mapping projectId is invalid.");
        if (!SiteKitProjectValidator.IsValidId(mapping.Alias)) errors.Add("mapping alias is invalid.");
        if (!SiteKitProjectValidator.IsValidId(mapping.EntityAlias)) errors.Add("mapping entityAlias is invalid.");
        if (string.IsNullOrWhiteSpace(mapping.Name) || mapping.Name.Length > 120) errors.Add("mapping name is required and must be at most 120 characters.");
        if (mapping.Format is not ("csv" or "json" or "xml")) errors.Add("mapping format must be csv, json or xml.");
        if (!string.IsNullOrWhiteSpace(mapping.SourceUrl))
        {
            if (!Uri.TryCreate(mapping.SourceUrl, UriKind.Absolute, out var sourceUri) || sourceUri.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(sourceUri.UserInfo) || !string.IsNullOrEmpty(sourceUri.Fragment) || sourceUri.Port != 443 || System.Net.IPAddress.TryParse(sourceUri.Host, out _))
                errors.Add("REST source must be an HTTPS URL on port 443 with a DNS hostname, and cannot contain credentials or a fragment.");
            if (mapping.Format != "json") errors.Add("REST sources must return JSON arrays and use the JSON format.");
        }
        if (mapping.Format == "csv" && (string.IsNullOrEmpty(mapping.Delimiter) || mapping.Delimiter.Length != 1 || mapping.Delimiter[0] is '\r' or '\n' or '"')) errors.Add("CSV delimiter must be one non-newline, non-quote character.");
        if (mapping.Fields is null || mapping.Fields.Count is 0 or > 100 || mapping.Fields.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Key.Length > 120 || !SiteKitProjectValidator.IsValidId(pair.Value)) || mapping.Fields.Values.Distinct(StringComparer.Ordinal).Count() != mapping.Fields.Count)
            errors.Add("mapping fields must map 1 to 100 unique entity field aliases from non-empty source names.");
        if (mapping.MatchFields is null || mapping.MatchFields.Length == 0 || mapping.MatchFields.Length > 10 || mapping.MatchFields.Distinct(StringComparer.Ordinal).Count() != mapping.MatchFields.Length || mapping.MatchFields.Any(field => !SiteKitProjectValidator.IsValidId(field)) || mapping.Fields is not null && mapping.MatchFields.Any(field => !mapping.Fields.Values.Contains(field, StringComparer.Ordinal)))
            errors.Add("mapping matchFields must identify one or more mapped entity field aliases.");
        return errors;
    }

    private static void ValidateProject(string id) { if (!SiteKitProjectValidator.IsValidId(id)) throw new ArgumentException("Project id is invalid.", nameof(id)); }
    private static void ValidateAlias(string alias) { if (!SiteKitProjectValidator.IsValidId(alias)) throw new ArgumentException("Import mapping alias is invalid.", nameof(alias)); }
}
