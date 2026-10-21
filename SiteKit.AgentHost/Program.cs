using System.Text.Json.Nodes;

namespace SiteKit.AgentHost;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var store = new CredentialStore(Environment.GetEnvironmentVariable("SITEKIT_AGENTHOST_DIRECTORY"));
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var siwc = new SiwcClient(http, store);
        var toolHost = new SiteKitToolHost();
        var responses = new ResponsesClient(http, siwc, toolHost);
        var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
        try
        {
            switch (command)
            {
                case "login":
                    Console.WriteLine("Continue with ChatGPT uses the official OpenAI sign-in flow. Sign-in identity and ChatGPT plan usage are separate permissions.");
                    Console.Write("Separately authorize SiteKit to use eligible ChatGPT plan usage for model requests? (y/N): ");
                    var authorizePlanUsage = string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase);
                    var account = await siwc.SignInAsync(authorizePlanUsage);
                    Console.WriteLine($"Connected ChatGPT account: {account.Email ?? account.Subject}");
                    Console.WriteLine(account.IsEligibleForPlanUsage ? "ChatGPT plan usage is authorized for this account." : "Identity is connected. ChatGPT plan usage is not authorized; AI operations remain unavailable until separately approved.");
                    Console.WriteLine($"Credentials are stored in the user-controlled host directory: {store.DirectoryPath}");
                    return 0;
                case "status":
                    var status = await store.LoadAsync();
                    Console.WriteLine(status is null ? "No ChatGPT account is connected." : $"Connected: {status.Email ?? status.Subject}\nPlan usage: {(status.IsEligibleForPlanUsage ? "authorized" : "not authorized")}\nToken expires: {status.ExpiresAt:u}\nHost id: {status.HostId}\nStorage: {store.DirectoryPath}");
                    return 0;
                case "models":
                    foreach (var model in await responses.ListModelsAsync()) Console.WriteLine($"{model.Slug}\t{model.DisplayName}");
                    return 0;
                case "logout":
                    await siwc.SignOutAsync();
                    Console.WriteLine("Local ChatGPT credentials cleared; remote revocation was attempted.");
                    return 0;
                case "mcp":
                    await new McpStdioServer(toolHost).RunAsync();
                    return 0;
                case "chat":
                    var projectId = GetOption(args, "--project") ?? "default-project";
                    var history = new AgentHistoryStore(Path.Combine(store.DirectoryPath, "history"));
                    var input = await history.LoadAsync(projectId);
                    Console.WriteLine($"SiteKit Architect · project {projectId}\nEnter /exit to end. Local conversation history is saved in {store.DirectoryPath}.");
                    while (true)
                    {
                        Console.Write("you> ");
                        var prompt = Console.ReadLine();
                        if (prompt is null || prompt.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase)) break;
                        if (string.IsNullOrWhiteSpace(prompt)) continue;
                        Console.Write("SiteKit> ");
                        try
                        {
                            var contextualPrompt = $"Current project id: {projectId}. User request: {prompt}";
                            await responses.CompleteTurnAsync(input, contextualPrompt);
                            Console.WriteLine();
                            await history.SaveAsync(projectId, input);
                        }
                        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or IOException)
                        {
                            Console.WriteLine("\nSiteKit could not complete that request: " + exception.Message);
                        }
                    }
                    return 0;
                default:
                    PrintHelp();
                    return command == "help" ? 0 : 2;
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or IOException or System.Security.Cryptography.CryptographicException or TimeoutException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static string? GetOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("SiteKit Agent Host (local, user-controlled runtime)");
        Console.WriteLine("  sitekit-agent login                 Continue with ChatGPT and separately choose plan usage");
        Console.WriteLine("  sitekit-agent status                 Show local connection and consent state");
        Console.WriteLine("  sitekit-agent models                 Discover models available to the connected account");
        Console.WriteLine("  sitekit-agent chat [--project ID]    Start a local AI orchestration session");
        Console.WriteLine("  sitekit-agent logout                 Revoke and clear the connected session");
        Console.WriteLine("  sitekit-agent mcp                    Run the local stdio MCP server");
        Console.WriteLine("Environment: SITEKIT_ROOT, SITEKIT_DATA_ROOT, SITEKIT_AGENTHOST_DIRECTORY");
    }
}
