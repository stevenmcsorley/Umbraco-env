using System.Text.Json;

namespace SiteKit.AgentHost;

/// <summary>Local stdio MCP endpoint for Codex and other compatible agent clients.</summary>
public sealed class McpStdioServer(SiteKitToolHost tools)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await Console.In.ReadLineAsync(cancellationToken);
            if (line is null) return;
            if (line.Length > 1_048_576) { Console.Error.WriteLine("Ignoring an MCP message larger than 1 MiB."); continue; }
            try
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                var method = root.TryGetProperty("method", out var methodValue) ? methodValue.GetString() : null;
                var hasId = root.TryGetProperty("id", out var id);
                if (!hasId) continue;
                var idJson = id.GetRawText();
                object? result = method switch
                {
                    "initialize" => Initialize(root),
                    "ping" => new { },
                    "tools/list" => new { tools = SiteKitToolHost.McpTools },
                    "tools/call" => await CallAsync(root, cancellationToken),
                    _ => null
                };
                if (result is null) await WriteAsync($"{{\"jsonrpc\":\"2.0\",\"id\":{idJson},\"error\":{{\"code\":-32601,\"message\":\"Method not found\"}}}}", cancellationToken);
                else
                {
                    var serialized = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    await WriteAsync($"{{\"jsonrpc\":\"2.0\",\"id\":{idJson},\"result\":{serialized}}}", cancellationToken);
                }
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
            {
                Console.Error.WriteLine("MCP request error: " + exception.Message);
            }
        }
    }

    private async Task<object> CallAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("params", out var parameters) || !parameters.TryGetProperty("name", out var nameValue))
            throw new ArgumentException("MCP tool call is missing params.name.");
        var name = nameValue.GetString() ?? throw new ArgumentException("MCP tool name is invalid.");
        var arguments = parameters.TryGetProperty("arguments", out var args) ? args : JsonDocument.Parse("{}").RootElement;
        if (arguments.ValueKind != JsonValueKind.Object) throw new ArgumentException("MCP tool arguments must be an object.");
        var result = await tools.ExecuteAsync(name, arguments, cancellationToken);
        var isError = result.Contains("\"ok\":false", StringComparison.Ordinal);
        return new { content = new[] { new { type = "text", text = result } }, isError };
    }

    private static object Initialize(JsonElement root)
    {
        var protocol = "2025-06-18";
        if (root.TryGetProperty("params", out var parameters) && parameters.TryGetProperty("protocolVersion", out var requested) && requested.ValueKind == JsonValueKind.String)
            protocol = requested.GetString()!;
        return new { protocolVersion = protocol, capabilities = new { tools = new { listChanged = false } }, serverInfo = new { name = "sitekit-agent-host", version = "0.1.0" } };
    }

    private static Task WriteAsync(string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.Out.WriteLine(message);
        Console.Out.Flush();
        return Task.CompletedTask;
    }
}
