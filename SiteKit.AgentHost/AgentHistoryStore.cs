using System.Text.Json;
using System.Text.Json.Nodes;

namespace SiteKit.AgentHost;

public sealed class AgentHistoryStore(string directory)
{
    private readonly string _directory = Path.GetFullPath(directory);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 48 };

    public async Task<List<JsonNode>> LoadAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var path = GetPath(projectId);
        if (!File.Exists(path)) return [];
        var info = new FileInfo(path);
        if (info.Length > 10 * 1024 * 1024) throw new InvalidDataException("Saved conversation history exceeds 10 MiB.");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<List<JsonNode>>(stream, Options, cancellationToken) ?? [];
    }

    public async Task SaveAsync(string projectId, List<JsonNode> history, CancellationToken cancellationToken = default)
    {
        if (history.Count > 10_000) throw new InvalidDataException("Conversation history has exceeded the item limit.");
        Directory.CreateDirectory(_directory);
        var path = GetPath(projectId);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous))
                await JsonSerializer.SerializeAsync(stream, history, Options, cancellationToken);
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        if (!OperatingSystem.IsWindows())
        {
            try { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
            catch (PlatformNotSupportedException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private string GetPath(string projectId)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(projectId, "^[a-z][a-z0-9-]{1,63}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
            throw new ArgumentException("Conversation project id is invalid.", nameof(projectId));
        return Path.Combine(_directory, projectId + ".conversation.json");
    }
}
