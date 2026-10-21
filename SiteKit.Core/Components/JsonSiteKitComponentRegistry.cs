using System.Text.Json;

namespace SiteKit.Core.Components;

/// <summary>Stores declarative, non-executable component definitions in the project's data directory.</summary>
public sealed class JsonSiteKitComponentRegistry : ISiteKitComponentRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitComponentRegistry(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = Path.GetFullPath(root);
    }

    public async Task<IReadOnlyList<SiteKitComponentDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_root)) return [];
        var results = new List<SiteKitComponentDefinition>();
        foreach (var file in Directory.EnumerateFiles(_root, "*.json", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var item = await ReadAsync(file, cancellationToken);
                if (item is not null && SiteKitComponentValidator.ValidateDefinition(item).Count == 0) results.Add(item);
            }
            catch (JsonException) { /* Ignore corrupt entries; callers can replace them by importing a valid version. */ }
            catch (IOException) { /* A concurrently replaced file can be retried by the next listing. */ }
        }
        return results.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Version, StringComparer.Ordinal).ToArray();
    }

    public async Task<SiteKitComponentDefinition?> GetAsync(string alias, string? version = null, CancellationToken cancellationToken = default)
    {
        ValidatePathPart(alias, nameof(alias));
        var directory = Path.Combine(_root, alias);
        if (!Directory.Exists(directory)) return null;
        if (version is not null)
        {
            ValidatePathPart(version, nameof(version));
            var exact = Path.Combine(directory, version + ".json");
            return File.Exists(exact) ? await ReadAsync(exact, cancellationToken) : null;
        }
        SiteKitComponentDefinition? latest = null;
        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            var candidate = await ReadAsync(file, cancellationToken);
            if (candidate is not null && (latest is null || CompareVersions(candidate.Version, latest.Version) > 0)) latest = candidate;
        }
        return latest;
    }

    public async Task<SiteKitComponentDefinition> SaveAsync(SiteKitComponentDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var errors = SiteKitComponentValidator.ValidateDefinition(definition);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(definition));
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await ValidateDependenciesAsync(definition, cancellationToken);
            var directory = Path.Combine(_root, definition.Alias);
            Directory.CreateDirectory(directory);
            var target = Path.Combine(directory, definition.Version + ".json");
            var saved = definition with { UpdatedAt = DateTimeOffset.UtcNow };
            var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                    await JsonSerializer.SerializeAsync(stream, saved, JsonOptions, cancellationToken);
                File.Move(temporary, target, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return saved;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DeleteAsync(string alias, string version, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePathPart(alias, nameof(alias));
        ValidatePathPart(version, nameof(version));
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.Combine(_root, alias);
            var file = Path.Combine(directory, version + ".json");
            if (!File.Exists(file)) return false;
            var remainingVersions = Directory.EnumerateFiles(directory, "*.json").Where(candidate => !string.Equals(candidate, file, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (remainingVersions.Length == 0)
            {
                var dependents = (await ListAsync(cancellationToken))
                    .Where(component => component.Alias != alias && component.Dependencies.Contains(alias, StringComparer.Ordinal))
                    .Select(component => component.Alias).Distinct(StringComparer.Ordinal).ToArray();
                if (dependents.Length > 0)
                    throw new InvalidOperationException($"Cannot remove the last version of '{alias}'; required by: {string.Join(", ", dependents)}.");
            }
            File.Delete(file);
            return true;
        }
        finally { _gate.Release(); }
    }

    public async Task<byte[]> ExportAsync(string alias, string version, CancellationToken cancellationToken = default)
    {
        var definition = await GetAsync(alias, version, cancellationToken) ?? throw new FileNotFoundException("Component version was not found.");
        return JsonSerializer.SerializeToUtf8Bytes(definition, JsonOptions);
    }

    public async Task<SiteKitComponentDefinition> ImportAsync(Stream package, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        using var bounded = new MemoryStream();
        await package.CopyToAsync(bounded, cancellationToken);
        if (bounded.Length > 1_048_576) throw new InvalidDataException("Component package exceeds the 1 MiB limit.");
        bounded.Position = 0;
        var definition = await JsonSerializer.DeserializeAsync<SiteKitComponentDefinition>(bounded, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("Component package is empty or invalid.");
        try { return await SaveAsync(definition, cancellationToken); }
        catch (ArgumentException exception) { throw new InvalidDataException(exception.Message, exception); }
    }

    private static async Task<SiteKitComponentDefinition?> ReadAsync(string file, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 81920, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<SiteKitComponentDefinition>(stream, JsonOptions, cancellationToken);
    }

    private static int CompareVersions(string left, string right)
        => Version.TryParse(left, out var leftVersion) && Version.TryParse(right, out var rightVersion)
            ? leftVersion.CompareTo(rightVersion) : StringComparer.Ordinal.Compare(left, right);

    private async Task ValidateDependenciesAsync(SiteKitComponentDefinition definition, CancellationToken cancellationToken)
    {
        foreach (var dependency in definition.Dependencies)
        {
            if (await GetAsync(dependency, cancellationToken: cancellationToken) is null)
                throw new InvalidOperationException($"Component '{definition.Alias}' requires missing component '{dependency}'. Import or create the dependency first.");
            if (await DependencyReachesAsync(dependency, definition.Alias, cancellationToken))
                throw new InvalidOperationException($"Component dependency cycle detected: '{definition.Alias}' -> '{dependency}' -> '{definition.Alias}'.");
        }
    }

    private async Task<bool> DependencyReachesAsync(string startAlias, string targetAlias, CancellationToken cancellationToken)
    {
        var pending = new Stack<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        pending.Push(startAlias);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var alias = pending.Pop();
            if (alias == targetAlias) return true;
            if (!visited.Add(alias)) continue;
            if (visited.Count > 256) throw new InvalidOperationException("Component dependency graph exceeds the 256 component limit.");
            var component = await GetAsync(alias, cancellationToken: cancellationToken);
            if (component is null) throw new InvalidOperationException($"Dependency '{alias}' is missing from the component registry.");
            if (SiteKitComponentValidator.ValidateDefinition(component).Count > 0)
                throw new InvalidOperationException($"Dependency '{alias}' has an invalid component definition.");
            foreach (var dependency in component.Dependencies) pending.Push(dependency);
        }
        return false;
    }

    private static void ValidatePathPart(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        if (value is "." or ".." || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("Value is not a valid path segment.", name);
    }
}
