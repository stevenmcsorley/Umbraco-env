using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SiteKit.Core.Components;

namespace SiteKit.Core.Modules;

/// <summary>Builds a deterministic, hash-verified declarative module ZIP from a local package source directory.</summary>
public static class SiteKitModulePackageBuilder
{
    private const int MaxPackageBytes = 10 * 1024 * 1024;
    private const int MaxFileBytes = 1024 * 1024;
    private const int MaxFiles = 255;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static async Task BuildAsync(string sourceDirectory, string outputZip, CancellationToken cancellationToken = default)
    {
        var source = Path.GetFullPath(sourceDirectory);
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException("Module source directory was not found.");
        var metadataPath = Path.Combine(source, "sitekit.module.json");
        var manifest = JsonSerializer.Deserialize<SiteKitModuleManifest>(await File.ReadAllBytesAsync(metadataPath, cancellationToken), Options)
            ?? throw new InvalidDataException("Module metadata is invalid.");
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        long total = 0;
        foreach (var path in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFullPath(path), metadataPath, StringComparison.OrdinalIgnoreCase)) continue;
            var relative = Path.GetRelativePath(source, path).Replace(Path.DirectorySeparatorChar, '/');
            JsonSiteKitModuleRegistry.ValidatePackagePath(relative);
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Module packages cannot contain symlinks or reparse points.");
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            if (bytes.Length > MaxFileBytes || (total += bytes.Length) > MaxPackageBytes) throw new InvalidDataException("Module package exceeds the file size limit.");
            files.Add(relative, bytes);
        }
        if (files.Count is 0 or > MaxFiles) throw new InvalidDataException("A module must contain between 1 and 255 content files.");
        var digests = files.ToDictionary(item => item.Key, item => Convert.ToHexString(SHA256.HashData(item.Value)).ToLowerInvariant(), StringComparer.Ordinal);
        var digestInput = new StringBuilder();
        foreach (var (path, hash) in digests) digestInput.Append(path).Append('\n').Append(hash).Append('\n');
        manifest = manifest with { PackageFiles = digests, IntegritySha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(digestInput.ToString()))).ToLowerInvariant() };
        var errors = JsonSiteKitModuleRegistry.ValidateManifest(manifest).ToList();
        foreach (var (path, bytes) in files) JsonSiteKitModuleRegistry.ValidateDeclarativeFile(path, bytes, errors);
        if (errors.Count != 0) throw new InvalidDataException(string.Join(" ", errors));
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            var manifestEntry = archive.CreateEntry("sitekit.module.json", CompressionLevel.Optimal);
            await using (var stream = manifestEntry.Open()) await JsonSerializer.SerializeAsync(stream, manifest, Options, cancellationToken);
            foreach (var (path, bytes) in files)
            {
                var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
                await using var stream = entry.Open();
                await stream.WriteAsync(bytes, cancellationToken);
            }
        }
        if (buffer.Length > MaxPackageBytes) throw new InvalidDataException("Compressed module archive exceeds the 10 MiB package limit.");
        var destination = Path.GetFullPath(outputZip);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temp, buffer.ToArray(), cancellationToken);
            File.Move(temp, destination, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
