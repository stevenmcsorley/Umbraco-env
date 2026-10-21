using System.Diagnostics;
using System.Text.Json;
using SiteKit.Core.Components;
using SiteKit.Core.Projects;

namespace SiteKit.AgentHost;

/// <summary>Typed, allowlisted local tools shared by the Agent Host and the stdio MCP server.</summary>
public sealed class SiteKitToolHost
{
    private readonly ISiteKitComponentRegistry _components;
    private readonly ISiteKitProjectRegistry _projects;
    private readonly ISiteKitPlacementRegistry _placements;
    private readonly string _root;
    private readonly string _auditPath;

    public SiteKitToolHost(string? root = null)
    {
        _root = Path.GetFullPath(root ?? Environment.GetEnvironmentVariable("SITEKIT_ROOT") ?? Directory.GetCurrentDirectory());
        var dataRoot = Environment.GetEnvironmentVariable("SITEKIT_DATA_ROOT") ??
            Path.Combine(_root, "MyDockerProject", "MyDockerProject", "umbraco", "Data", "SiteKit");
        _components = new JsonSiteKitComponentRegistry(Path.Combine(dataRoot, "components"));
        _projects = new JsonSiteKitProjectRegistry(Path.Combine(dataRoot, "projects"));
        _placements = new JsonSiteKitPlacementRegistry(Path.Combine(dataRoot, "placements"));
        _auditPath = Path.Combine(dataRoot, "audit", "agent-tools.jsonl");
    }

    public static IReadOnlyList<object> ResponseTools =>
    [
        Function("sitekit_list_components", "List reusable component definitions and versions."),
        Function("sitekit_create_component", "Create or update a validated declarative component version.", new { definition = Schema("object") }, "definition"),
        Function("sitekit_list_projects", "List independent SiteKit projects."),
        Function("sitekit_inspect_project", "Inspect one project manifest and its component placements.", new { projectId = Schema("string") }, "projectId"),
        Function("sitekit_create_project", "Create or update a project manifest.", new { project = Schema("object") }, "project"),
        Function("sitekit_create_placement", "Place an existing reusable component in a project with independent data and appearance.", new { projectId = Schema("string"), placement = Schema("object") }, "projectId", "placement"),
        Function("sitekit_validate_project", "Validate the project's module, component and placement references without changing data.", new { projectId = Schema("string") }, "projectId"),
        Function("sitekit_build_project", "Run the repository's fixed UmbraccoEnv.sln build; this tool accepts no shell command or path.", new { projectId = Schema("string") }, "projectId")
    ];

    public static IReadOnlyList<object> McpTools =>
    [
        McpTool("sitekit.list_projects", "List independent SiteKit projects.", EmptySchema()),
        McpTool("sitekit.list_components", "List reusable component definitions and versions.", EmptySchema()),
        McpTool("sitekit.inspect_project", "Inspect one project manifest and placements.", ObjectSchema("projectId")),
        McpTool("sitekit.create_project", "Create or update a project manifest.", ObjectSchema("project")),
        McpTool("sitekit.create_component", "Create a validated declarative component version.", ObjectSchema("definition")),
        McpTool("sitekit.create_placement", "Place a shared component into a project with local styling and data.", ObjectSchema("projectId", "placement")),
        McpTool("sitekit.validate_project", "Validate component and placement references.", ObjectSchema("projectId")),
        McpTool("sitekit.build_project", "Build the fixed SiteKit solution using dotnet; no arbitrary shell commands are accepted.", ObjectSchema("projectId"))
    ];

    public async Task<string> ExecuteAsync(string name, JsonElement arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = name switch
            {
                "sitekit_list_components" or "sitekit.list_components" => await ListComponentsAsync(cancellationToken),
                "sitekit_create_component" or "sitekit.create_component" => await CreateComponentAsync(arguments, cancellationToken),
                "sitekit_list_projects" or "sitekit.list_projects" => await ListProjectsAsync(cancellationToken),
                "sitekit_inspect_project" or "sitekit.inspect_project" => await InspectProjectAsync(arguments, cancellationToken),
                "sitekit_create_project" or "sitekit.create_project" => await CreateProjectAsync(arguments, cancellationToken),
                "sitekit_create_placement" or "sitekit.create_placement" => await CreatePlacementAsync(arguments, cancellationToken),
                "sitekit_validate_project" or "sitekit.validate_project" => await ValidateProjectAsync(arguments, cancellationToken),
                "sitekit_build_project" or "sitekit.build_project" => await BuildProjectAsync(arguments, cancellationToken),
                _ => throw new InvalidOperationException("This SiteKit tool is not enabled.")
            };
            await AuditAsync(name, true, cancellationToken);
            return JsonSerializer.Serialize(new { ok = true, result });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or JsonException or IOException or UnauthorizedAccessException)
        {
            await AuditAsync(name, false, cancellationToken);
            return JsonSerializer.Serialize(new { ok = false, error = exception.Message });
        }
    }

    private async Task<object> ListComponentsAsync(CancellationToken ct) => await _components.ListAsync(ct);
    private async Task<object> ListProjectsAsync(CancellationToken ct) => await _projects.ListAsync(ct);

    private async Task<object> InspectProjectAsync(JsonElement args, CancellationToken ct)
    {
        var id = RequiredString(args, "projectId");
        var project = await _projects.GetAsync(id, ct) ?? throw new InvalidOperationException("Project not found.");
        return new { project, placements = await _placements.ListAsync(id, ct) };
    }

    private async Task<object> CreateProjectAsync(JsonElement args, CancellationToken ct)
    {
        var manifest = Deserialize<SiteKitProjectManifest>(RequiredObject(args, "project"));
        var errors = SiteKitProjectValidator.Validate(manifest);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        return await _projects.SaveAsync(manifest, ct);
    }

    private async Task<object> CreateComponentAsync(JsonElement args, CancellationToken ct)
    {
        var definition = Deserialize<SiteKitComponentDefinition>(RequiredObject(args, "definition"));
        var errors = SiteKitComponentValidator.ValidateDefinition(definition);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        return await _components.SaveAsync(definition, ct);
    }

    private async Task<object> CreatePlacementAsync(JsonElement args, CancellationToken ct)
    {
        var projectId = RequiredString(args, "projectId");
        if (await _projects.GetAsync(projectId, ct) is null) throw new InvalidOperationException("Project not found.");
        var placement = Deserialize<SiteKitComponentPlacement>(RequiredObject(args, "placement"));
        var errors = JsonSiteKitPlacementRegistry.ValidatePlacement(placement).ToList();
        var component = await _components.GetAsync(placement.ComponentAlias, placement.Version, ct);
        if (component is null) errors.Add("The referenced component was not found.");
        else
        {
            errors.AddRange(SiteKitComponentValidator.ValidateData(component, placement.Data));
            if (placement.Variant is not null && component.Variants.All(x => x.Alias != placement.Variant)) errors.Add("The referenced component variant was not found.");
        }
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        return await _placements.SaveAsync(projectId, placement, ct);
    }

    private async Task<object> ValidateProjectAsync(JsonElement args, CancellationToken ct)
    {
        var id = RequiredString(args, "projectId");
        var project = await _projects.GetAsync(id, ct) ?? throw new InvalidOperationException("Project not found.");
        var errors = new List<string>();
        foreach (var alias in project.Components)
            if (await _components.GetAsync(alias, cancellationToken: ct) is null) errors.Add($"Component '{alias}' is not installed.");
        foreach (var placement in await _placements.ListAsync(id, ct))
        {
            var component = await _components.GetAsync(placement.ComponentAlias, placement.Version, ct);
            if (component is null) errors.Add($"Placement '{placement.Id}' references a missing component.");
            else errors.AddRange(SiteKitComponentValidator.ValidateData(component, placement.Data).Select(error => $"Placement '{placement.Id}': {error}"));
        }
        return new { valid = errors.Count == 0, errors };
    }

    private async Task<object> BuildProjectAsync(JsonElement args, CancellationToken ct)
    {
        var id = RequiredString(args, "projectId");
        if (await _projects.GetAsync(id, ct) is null) throw new InvalidOperationException("Project not found.");
        var solution = Path.Combine(_root, "UmbraccoEnv.sln");
        if (!File.Exists(solution)) throw new InvalidOperationException("UmbraccoEnv.sln was not found under the configured SiteKit root.");
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = _root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("build");
        start.ArgumentList.Add(solution);
        start.ArgumentList.Add("--no-restore");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the dotnet build process.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } throw; }
        var output = await stdout;
        var error = await stderr;
        var tail = (output + Environment.NewLine + error).Split(Environment.NewLine).TakeLast(50).ToArray();
        if (process.ExitCode != 0) throw new InvalidOperationException($"Build failed with exit code {process.ExitCode}:\n{string.Join(Environment.NewLine, tail)}");
        return new { projectId = id, exitCode = process.ExitCode, outputTail = tail };
    }

    private async Task AuditAsync(string tool, bool success, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_auditPath)!);
        var line = JsonSerializer.Serialize(new { at = DateTimeOffset.UtcNow, tool, success }) + Environment.NewLine;
        await File.AppendAllTextAsync(_auditPath, line, ct);
    }

    private static T Deserialize<T>(JsonElement element) => element.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new JsonException("Tool input is not a valid object.");
    private static JsonElement RequiredObject(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : throw new ArgumentException($"`{name}` must be an object.");
    private static string RequiredString(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()! : throw new ArgumentException($"`{name}` is required.");

    private static object Function(string name, string description, object? properties = null, params string[] required)
        => new { type = "function", name, description, parameters = new { type = "object", properties = properties ?? new { }, required, additionalProperties = false } };

    private static object Schema(string type) => new { type };
    private static object McpTool(string name, string description, object inputSchema) => new { name, description, inputSchema };
    private static object EmptySchema() => new { type = "object", properties = new { }, additionalProperties = false };
    private static object ObjectSchema(params string[] properties)
    {
        var defs = properties.ToDictionary(x => x, x => (object)new { type = x.EndsWith("Id", StringComparison.Ordinal) ? "string" : "object" }, StringComparer.Ordinal);
        return new { type = "object", properties = defs, required = properties, additionalProperties = false };
    }
}
