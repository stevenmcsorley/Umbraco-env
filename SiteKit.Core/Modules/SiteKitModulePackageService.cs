using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SiteKit.Core.Components;
using SiteKit.Core.Data;

namespace SiteKit.Core.Modules;

public interface ISiteKitModuleRegistry
{
    Task<IReadOnlyList<SiteKitModuleManifest>> ListAsync(CancellationToken cancellationToken = default);
    Task<SiteKitModuleManifest?> GetAsync(string id, string version, CancellationToken cancellationToken = default);
    Task<SiteKitModuleManifest> ImportAsync(Stream package, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAsync(string id, string version, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, string version, CancellationToken cancellationToken = default);
}

/// <summary>Verifies bounded, declarative module ZIP packages. It never loads or executes package code.</summary>
public sealed class JsonSiteKitModuleRegistry : ISiteKitModuleRegistry
{
    private const int MaxPackageBytes = 10 * 1024 * 1024;
    private const int MaxFileBytes = 1024 * 1024;
    private const int MaxFiles = 256;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitModuleRegistry(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<SiteKitModuleManifest>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_root)) return [];
        var modules = new List<SiteKitModuleManifest>();
        foreach (var path in Directory.EnumerateFiles(_root, "sitekit.module.json", SearchOption.AllDirectories))
        {
            await using var input = File.OpenRead(path);
            var manifest = await JsonSerializer.DeserializeAsync<SiteKitModuleManifest>(input, JsonOptions, cancellationToken);
            if (manifest is not null && ValidateManifest(manifest).Count == 0) modules.Add(manifest);
        }
        return modules.OrderBy(module => module.Id, StringComparer.Ordinal).ThenBy(module => module.Version, StringComparer.Ordinal).ToArray();
    }

    public async Task<SiteKitModuleManifest?> GetAsync(string id, string version, CancellationToken cancellationToken = default)
    {
        ValidateId(id); ValidateVersion(version);
        var path = Path.Combine(_root, id, version, "sitekit.module.json");
        if (!File.Exists(path)) return null;
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<SiteKitModuleManifest>(input, JsonOptions, cancellationToken);
    }

    public async Task<SiteKitModuleManifest> ImportAsync(Stream package, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await package.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxPackageBytes) throw new InvalidDataException("Module package must be no larger than 10 MiB.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
        if (buffer.Length == 0) throw new InvalidDataException("Module package cannot be empty.");
        buffer.Position = 0;
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, true);
        if (archive.Entries.Count is < 1 or > MaxFiles) throw new InvalidDataException("Module package contains an invalid number of files.");
        var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        long total = 0;
        foreach (var entry in archive.Entries)
        {
            var path = entry.FullName;
            ValidatePackagePath(path);
            if (entry.Length > MaxFileBytes || (total += entry.Length) > MaxPackageBytes) throw new InvalidDataException("Module package exceeds the file size limit.");
            if (entries.ContainsKey(path)) throw new InvalidDataException("Module package contains duplicate paths.");
            await using var input = entry.Open();
            using var file = new MemoryStream((int)entry.Length);
            await input.CopyToAsync(file, cancellationToken);
            entries[path] = file.ToArray();
        }
        if (!entries.TryGetValue("sitekit.module.json", out var manifestBytes)) throw new InvalidDataException("Module package must contain sitekit.module.json at its root.");
        var manifest = JsonSerializer.Deserialize<SiteKitModuleManifest>(manifestBytes, JsonOptions) ?? throw new InvalidDataException("Module manifest is invalid.");
        var errors = ValidateManifest(manifest).ToList();
        var contentPaths = entries.Keys.Where(path => path != "sitekit.module.json").ToHashSet(StringComparer.Ordinal);
        if (!contentPaths.SetEquals(manifest.PackageFiles.Keys)) errors.Add("Package files do not match the manifest integrity list.");
        var digestInput = new StringBuilder();
        foreach (var path in manifest.PackageFiles.Keys.Order(StringComparer.Ordinal))
        {
            if (!entries.TryGetValue(path, out var bytes)) continue;
            var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (!string.Equals(digest, manifest.PackageFiles[path], StringComparison.OrdinalIgnoreCase)) errors.Add($"Package file '{path}' failed its SHA-256 check.");
            digestInput.Append(path).Append('\n').Append(digest).Append('\n');
            ValidateDeclarativeFile(path, bytes, errors);
        }
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(digestInput.ToString()))).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes((manifest.IntegritySha256 ?? string.Empty).ToLowerInvariant())))
            errors.Add("Module package integrity hash is invalid.");
        if (errors.Count != 0) throw new InvalidDataException(string.Join(" ", errors));

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var destination = Path.Combine(_root, manifest.Id, manifest.Version);
            if (Directory.Exists(destination)) throw new InvalidOperationException("This module version is already installed.");
            var staging = Path.Combine(_root, ".staging-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(staging);
                foreach (var (path, bytes) in entries)
                {
                    var filePath = Path.Combine(staging, path.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                Directory.Move(staging, destination);
            }
            finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
        }
        finally { _gate.Release(); }
        return manifest;
    }

    public async Task<byte[]> ExportAsync(string id, string version, CancellationToken cancellationToken = default)
    {
        var manifest = await GetAsync(id, version, cancellationToken) ?? throw new FileNotFoundException("Module version was not found.");
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            var root = Path.Combine(_root, id, version);
            foreach (var relative in manifest.PackageFiles.Keys.Order(StringComparer.Ordinal).Prepend("sitekit.module.json"))
            {
                var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path)) throw new InvalidDataException($"Installed module file '{relative}' is missing.");
                var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
                await using var destination = entry.Open();
                await using var source = File.OpenRead(path);
                await source.CopyToAsync(destination, cancellationToken);
            }
        }
        return output.ToArray();
    }

    public async Task<bool> DeleteAsync(string id, string version, CancellationToken cancellationToken = default)
    {
        ValidateId(id); ValidateVersion(version);
        var source = Path.GetFullPath(Path.Combine(_root, id, version));
        var root = Path.GetFullPath(_root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var rootWithSeparator = root + Path.DirectorySeparatorChar;
        var relative = Path.GetRelativePath(root, source);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !source.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Module package path is invalid.");
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!Directory.Exists(source)) return false;
            var tombstone = Path.Combine(_root, ".deleting-" + Guid.NewGuid().ToString("N"));
            Directory.Move(source, tombstone);
            try { Directory.Delete(tombstone, recursive: true); }
            catch
            {
                if (Directory.Exists(tombstone) && !Directory.Exists(source)) Directory.Move(tombstone, source);
                throw;
            }
            return true;
        }
        finally { _gate.Release(); }
    }

    public static IReadOnlyList<string> ValidateManifest(SiteKitModuleManifest manifest)
    {
        var errors = new List<string>();
        if (manifest.SchemaVersion != 1) errors.Add("Module schemaVersion must be 1.");
        if (!IsId(manifest.Id)) errors.Add("Module id is invalid.");
        if (!Regex.IsMatch(manifest.Version ?? "", "^[0-9]+\\.[0-9]+\\.[0-9]+(?:-[0-9A-Za-z.-]+)?$")) errors.Add("Module version must use semantic version syntax.");
        if (string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 120) errors.Add("Module name is required.");
        if (manifest.CompatibleSiteKitVersions is null || manifest.CompatibleSiteKitVersions.Length == 0 || manifest.CompatibleSiteKitVersions.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 100)) errors.Add("CompatibleSiteKitVersions must declare at least one supported version.");
        if (manifest.Dependencies is null || manifest.Dependencies.Any(dependency => !IsId(dependency)) || manifest.Dependencies.Contains(manifest.Id, StringComparer.Ordinal)) errors.Add("Module dependencies contain an invalid id or self-reference.");
        if (manifest.DependencyVersions is null) errors.Add("Module dependencyVersions are required.");
        else if (manifest.Dependencies is null || manifest.DependencyVersions.Any(pair => !manifest.Dependencies.Contains(pair.Key, StringComparer.Ordinal) || !SiteKitModuleVersion.IsValidRange(pair.Value)))
            errors.Add("Module dependencyVersions must provide valid ranges only for declared dependencies.");
        if (manifest.ProjectCategories is null || manifest.ProjectCategories.Length == 0 || manifest.ProjectCategories.Any(category => category is not ("website" or "application" or "combined" or "api" or "content"))) errors.Add("Module project categories are invalid.");
        if (manifest.PackageFiles is null || manifest.PackageFiles.Count > MaxFiles - 1) errors.Add("Module package file list is invalid.");
        else foreach (var (path, hash) in manifest.PackageFiles)
        {
            try { ValidatePackagePath(path); }
            catch (InvalidDataException exception) { errors.Add(exception.Message); }
            if (!Regex.IsMatch(hash ?? "", "^[0-9a-fA-F]{64}$")) errors.Add($"Package file '{path}' has an invalid SHA-256 value.");
        }
        if (!Regex.IsMatch(manifest.IntegritySha256 ?? "", "^[0-9a-fA-F]{64}$")) errors.Add("Module integrity SHA-256 is invalid.");
        return errors;
    }

    internal static void ValidateDeclarativeFile(string path, byte[] bytes, List<string> errors)
    {
        if (path.StartsWith("components/", StringComparison.Ordinal) && path.EndsWith(".json", StringComparison.Ordinal))
        {
            try
            {
                var definition = JsonSerializer.Deserialize<SiteKitComponentDefinition>(bytes, JsonOptions);
                if (definition is null) errors.Add($"Component '{path}' is empty.");
                else errors.AddRange(SiteKitComponentValidator.ValidateDefinition(definition).Select(error => $"{path}: {error}"));
            }
            catch (JsonException) { errors.Add($"Component '{path}' is invalid JSON."); }
            return;
        }
        if (path.StartsWith("entities/", StringComparison.Ordinal) && path.EndsWith(".json", StringComparison.Ordinal))
        {
            try
            {
                var definition = JsonSerializer.Deserialize<SiteKitEntityDefinition>(bytes, JsonOptions);
                if (definition is null) errors.Add($"Entity '{path}' is empty.");
                else errors.AddRange(SiteKitEntityValidator.Validate(definition).Select(error => $"{path}: {error}"));
            }
            catch (JsonException) { errors.Add($"Entity '{path}' is invalid JSON."); }
            return;
        }
        if (path.StartsWith("screens/", StringComparison.Ordinal) && path.EndsWith(".json", StringComparison.Ordinal))
        {
            try
            {
                var definition = JsonSerializer.Deserialize<SiteKitApplicationScreenDefinition>(bytes, JsonOptions);
                if (definition is null) errors.Add($"Screen '{path}' is empty.");
                else errors.AddRange(SiteKitScreenValidator.Validate(definition).Select(error => $"{path}: {error}"));
            }
            catch (JsonException) { errors.Add($"Screen '{path}' is invalid JSON."); }
            return;
        }
        if (!(path.StartsWith("screens/", StringComparison.Ordinal) || path.StartsWith("workflows/", StringComparison.Ordinal)) &&
            !(path.StartsWith("docs/", StringComparison.Ordinal) && path.EndsWith(".md", StringComparison.Ordinal)))
        {
            errors.Add($"Package file '{path}' is not a supported declarative content type; executable source and web assets are not accepted.");
            return;
        }
        if (path.EndsWith(".json", StringComparison.Ordinal))
        {
            try { using var _ = JsonDocument.Parse(bytes); }
            catch (JsonException) { errors.Add($"Package file '{path}' is invalid JSON."); }
        }
        else if (!path.StartsWith("docs/", StringComparison.Ordinal) || !path.EndsWith(".md", StringComparison.Ordinal))
            errors.Add($"Package file '{path}' must be JSON or documentation markdown.");
    }

    internal static void ValidatePackagePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 240 || path.StartsWith('/') || path.Contains('\\') || path.Split('/').Any(segment => segment is "" or "." or "..") || path.Contains(':'))
            throw new InvalidDataException("Module package contains an unsafe file path.");
    }

    private static bool IsId(string? id) => id is not null && Regex.IsMatch(id, "^[a-z][a-z0-9-]{1,63}$");
    private static void ValidateId(string id) { if (!IsId(id)) throw new ArgumentException("Module id is invalid.", nameof(id)); }
    private static void ValidateVersion(string version) { if (!Regex.IsMatch(version, "^[0-9]+\\.[0-9]+\\.[0-9]+(?:-[0-9A-Za-z.-]+)?$")) throw new ArgumentException("Module version is invalid.", nameof(version)); }
}
