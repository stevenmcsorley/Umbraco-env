using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SiteKit.AgentHost;

public sealed class CredentialStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private const string ProtectedPrefix = "SITEKIT-DPAPI-V1:";
    private readonly string _directory;
    private readonly string _credentialsPath;
    private readonly string _hostIdPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CredentialStore(string? directory = null)
    {
        _directory = Path.GetFullPath(directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SiteKit", "AgentHost"));
        _credentialsPath = Path.Combine(_directory, "chatgpt-account.json");
        _hostIdPath = Path.Combine(_directory, "host-id.txt");
    }

    public string DirectoryPath => _directory;

    public async Task<string> GetOrCreateHostIdAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_directory);
        if (File.Exists(_hostIdPath))
        {
            var value = (await File.ReadAllTextAsync(_hostIdPath, cancellationToken)).Trim();
            if (value.StartsWith("urn:uuid:", StringComparison.Ordinal) && Guid.TryParse(value[9..], out _)) return value;
        }
        var hostId = "urn:uuid:" + Guid.NewGuid().ToString("D");
        await WriteAtomicallyAsync(_hostIdPath, hostId, cancellationToken);
        RestrictPermissions(_hostIdPath, directory: false);
        return hostId;
    }

    public async Task<ChatGptAccount?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_credentialsPath)) return null;
        var stored = await File.ReadAllTextAsync(_credentialsPath, cancellationToken);
        if (stored.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
        {
            if (!OperatingSystem.IsWindows()) throw new CryptographicException("This ChatGPT credential file was protected for Windows and cannot be opened on this operating system.");
            var protectedBytes = Convert.FromBase64String(stored[ProtectedPrefix.Length..]);
            stored = Encoding.UTF8.GetString(ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser));
        }
        return JsonSerializer.Deserialize<ChatGptAccount>(stored, Options);
    }

    public async Task SaveAsync(ChatGptAccount account, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(_directory);
            RestrictPermissions(_directory, directory: true);
            var json = JsonSerializer.Serialize(account, Options);
            if (OperatingSystem.IsWindows())
            {
                var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), optionalEntropy: null, DataProtectionScope.CurrentUser);
                json = ProtectedPrefix + Convert.ToBase64String(protectedBytes);
            }
            await WriteAtomicallyAsync(_credentialsPath, json, cancellationToken);
            RestrictPermissions(_credentialsPath, directory: false);
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { if (File.Exists(_credentialsPath)) File.Delete(_credentialsPath); }
        finally { _gate.Release(); }
    }

    private static async Task WriteAtomicallyAsync(string path, string value, CancellationToken cancellationToken)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous))
            await using (var writer = new StreamWriter(stream))
                await writer.WriteAsync(value.AsMemory(), cancellationToken);
            RestrictPermissions(temp, directory: false);
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void RestrictPermissions(string path, bool directory)
    {
        if (OperatingSystem.IsWindows()) return; // The default per-user application-data directory inherits the current user's ACL.
        try
        {
            File.SetUnixFileMode(path, directory
                ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                : UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (PlatformNotSupportedException) { }
        catch (UnauthorizedAccessException) { }
    }
}
