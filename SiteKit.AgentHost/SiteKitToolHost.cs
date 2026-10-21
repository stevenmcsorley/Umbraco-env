using System.Diagnostics;
using System.Text.Json;
using SiteKit.Core.Components;
using SiteKit.Core.Projects;
using SiteKit.Core.Data;
using SiteKit.Core.Modules;

namespace SiteKit.AgentHost;

/// <summary>Typed, allowlisted local tools shared by the Agent Host and the stdio MCP server.</summary>
public sealed class SiteKitToolHost
{
    private readonly ISiteKitComponentRegistry _components;
    private readonly ISiteKitProjectRegistry _projects;
    private readonly ISiteKitPlacementRegistry _placements;
    private readonly ISiteKitEntityRegistry _entities;
    private readonly ISiteKitScreenRegistry _screens;
    private readonly SiteKitProjectGenerator _generator;
    private readonly ISiteKitModuleRegistry _modules;
    private readonly SiteKitModuleInstaller _moduleInstaller;
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
        _entities = new JsonSiteKitEntityRegistry(Path.Combine(dataRoot, "entities"));
        _screens = new JsonSiteKitScreenRegistry(Path.Combine(dataRoot, "screens"));
        _modules = new JsonSiteKitModuleRegistry(Path.Combine(dataRoot, "modules"));
        _generator = new SiteKitProjectGenerator(Path.Combine(_root, "generated-projects"), _projects, _screens, _entities, _components, _placements);
        _moduleInstaller = new SiteKitModuleInstaller(Path.Combine(dataRoot, "modules"), _modules, _projects, _components, _entities, _screens);
        _auditPath = Path.Combine(dataRoot, "audit", "agent-tools.jsonl");
    }

    public static IReadOnlyList<object> ResponseTools =>
    [
        Function("sitekit_list_components", "List reusable component definitions and versions."),
        Function("sitekit_create_component", "Create or update a validated declarative component version.", new { definition = Schema("object") }, "definition"),
        Function("sitekit_list_projects", "List independent SiteKit projects."),
        Function("sitekit_inspect_project", "Inspect one project manifest and its component placements.", new { projectId = Schema("string") }, "projectId"),
        Function("sitekit_create_project", "Create or update a project manifest.", new { project = Schema("object") }, "project"),
        Function("sitekit_generate_project", "Generate an independent deployable .NET project from an existing SiteKit manifest.", new { projectId = Schema("string") }, "projectId"),
        Function("sitekit_create_entity", "Create or update a typed operational data model for a SiteKit project. Relationship fields reference another entity alias.", new { definition = Schema("object") }, "definition"),
        Function("sitekit_list_entities", "List operational data models for a SiteKit project.", new { projectId = Schema("string") }, "projectId"),
        Function("sitekit_create_screen", "Create or update a validated data-connected application screen.", new { screen = Schema("object") }, "screen"),
        Function("sitekit_list_screens", "List application screens in a project.", new { projectId = Schema("string") }, "projectId"),
        Function("sitekit_list_modules", "List verified declarative modules installed in the local package registry."),
        Function("sitekit_install_module", "Install a verified declarative module and its schemas into a project.", new { projectId = Schema("string"), moduleId = Schema("string"), version = Schema("string") }, "projectId", "moduleId", "version"),
        Function("sitekit_upgrade_module", "Explicitly upgrade a project's pinned module version. Downgrades are rejected.", new { projectId = Schema("string"), moduleId = Schema("string"), version = Schema("string") }, "projectId", "moduleId", "version"),
        Function("sitekit_create_placement", "Place an existing reusable component in a project with independent data and appearance.", new { projectId = Schema("string"), placement = Schema("object") }, "projectId", "placement"),
        Function("sitekit_preview_project", "Render all validated component placements in a project for an HTML preview.", new { projectId = Schema("string") }, "projectId"),
        Function("sitekit_validate_project", "Validate the project's module, component and placement references without changing data.", new { projectId = Schema("string") }, "projectId"),
        Function("sitekit_build_project", "Build this project's generated application when available, otherwise build the fixed SiteKit solution. No arbitrary command or path is accepted.", new { projectId = Schema("string") }, "projectId")
    ];

    public static IReadOnlyList<object> McpTools =>
    [
        McpTool("sitekit.list_projects", "List independent SiteKit projects.", EmptySchema()),
        McpTool("sitekit.list_components", "List reusable component definitions and versions.", EmptySchema()),
        McpTool("sitekit.inspect_project", "Inspect one project manifest and placements.", ObjectSchema("projectId")),
        McpTool("sitekit.create_project", "Create or update a project manifest.", ObjectSchema("project")),
        McpTool("sitekit.generate_project", "Generate an independent deployable .NET project.", ObjectSchema("projectId")),
        McpTool("sitekit.create_entity", "Create a typed operational data model.", ObjectSchema("definition")),
        McpTool("sitekit.list_entities", "List project operational data models.", ObjectSchema("projectId")),
        McpTool("sitekit.create_screen", "Create or update a validated application screen.", ObjectSchema("screen")),
        McpTool("sitekit.list_screens", "List application screens in a project.", ObjectSchema("projectId")),
        McpTool("sitekit.list_modules", "List verified local modules.", EmptySchema()),
        McpTool("sitekit.install_module", "Install an existing verified declarative module.", ObjectSchema("projectId", "moduleId", "version")),
        McpTool("sitekit.upgrade_module", "Explicitly upgrade a project's pinned module version; downgrades are rejected.", ObjectSchema("projectId", "moduleId", "version")),
        McpTool("sitekit.create_component", "Create a validated declarative component version.", ObjectSchema("definition")),
        McpTool("sitekit.create_placement", "Place a shared component into a project with local styling and data.", ObjectSchema("projectId", "placement")),
        McpTool("sitekit.preview_project", "Render validated component placements for a project.", ObjectSchema("projectId")),
        McpTool("sitekit.validate_project", "Validate component and placement references.", ObjectSchema("projectId")),
        McpTool("sitekit.build_project", "Build this project's generated application when available, otherwise the fixed SiteKit solution; no arbitrary shell command or path is accepted.", ObjectSchema("projectId"))
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
                "sitekit_generate_project" or "sitekit.generate_project" => await GenerateProjectAsync(arguments, cancellationToken),
                "sitekit_create_entity" or "sitekit.create_entity" => await CreateEntityAsync(arguments, cancellationToken),
                "sitekit_list_entities" or "sitekit.list_entities" => await ListEntitiesAsync(arguments, cancellationToken),
                "sitekit_create_screen" or "sitekit.create_screen" => await CreateScreenAsync(arguments, cancellationToken),
                "sitekit_list_screens" or "sitekit.list_screens" => await ListScreensAsync(arguments, cancellationToken),
                "sitekit_list_modules" or "sitekit.list_modules" => await _modules.ListAsync(cancellationToken),
                "sitekit_install_module" or "sitekit.install_module" => await InstallModuleAsync(arguments, cancellationToken),
                "sitekit_upgrade_module" or "sitekit.upgrade_module" => await UpgradeModuleAsync(arguments, cancellationToken),
                "sitekit_create_placement" or "sitekit.create_placement" => await CreatePlacementAsync(arguments, cancellationToken),
                "sitekit_preview_project" or "sitekit.preview_project" => await PreviewProjectAsync(arguments, cancellationToken),
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

    private async Task<object> GenerateProjectAsync(JsonElement args, CancellationToken ct)
    {
        var id = RequiredString(args, "projectId");
        var directory = await _generator.GenerateAsync(id, ct);
        return new { projectId = id, directory, buildCommand = $"dotnet build {id}.csproj", runCommand = $"dotnet run --project {id}.csproj" };
    }

    private async Task<object> CreateEntityAsync(JsonElement args, CancellationToken ct)
    {
        var definition = Deserialize<SiteKitEntityDefinition>(RequiredObject(args, "definition"));
        if (await _projects.GetAsync(definition.ProjectId, ct) is null) throw new InvalidOperationException("Project not found.");
        var errors = SiteKitEntityValidator.Validate(definition);
        foreach (var field in definition.Fields.Where(field => field.Type is "relationship" or "relationships" && SiteKitProjectValidator.IsValidId(field.RelatedEntity)))
            if (field.RelatedEntity != definition.Alias && await _entities.GetAsync(definition.ProjectId, field.RelatedEntity!, ct) is null) errors = errors.Append($"Related entity '{field.RelatedEntity}' is not defined.").ToArray();
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        var saved = await _entities.SaveAsync(definition, ct);
        var project = await _projects.GetAsync(definition.ProjectId, ct);
        if (project is not null && !project.Entities.Contains(definition.Alias, StringComparer.Ordinal))
            await _projects.SaveAsync(project with { Entities = project.Entities.Append(definition.Alias).ToArray() }, ct);
        return saved;
    }

    private async Task<object> ListEntitiesAsync(JsonElement args, CancellationToken ct)
    {
        var projectId = RequiredString(args, "projectId");
        if (await _projects.GetAsync(projectId, ct) is null) throw new InvalidOperationException("Project not found.");
        return await _entities.ListAsync(projectId, ct);
    }

    private async Task<object> CreateScreenAsync(JsonElement args, CancellationToken ct)
    {
        var screen = Deserialize<SiteKitApplicationScreenDefinition>(RequiredObject(args, "screen"));
        if (await _projects.GetAsync(screen.ProjectId, ct) is null) throw new InvalidOperationException("Project not found.");
        var entity = screen.EntityAlias is null ? null : await _entities.GetAsync(screen.ProjectId, screen.EntityAlias, ct);
        if (screen.EntityAlias is not null && entity is null) throw new InvalidOperationException("The referenced entity is not defined in this project.");
        var errors = SiteKitScreenValidator.Validate(screen, entity).ToList();
        foreach (var component in screen.Components ?? [])
            if (await _components.GetAsync(component, cancellationToken: ct) is null) errors.Add($"Component '{component}' is not installed.");
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        var saved = await _screens.SaveAsync(screen, ct);
        var project = await _projects.GetAsync(screen.ProjectId, ct);
        if (project is not null && !project.Screens.Contains(screen.Alias, StringComparer.Ordinal))
            await _projects.SaveAsync(project with { Screens = project.Screens.Append(screen.Alias).ToArray() }, ct);
        return saved;
    }

    private async Task<object> ListScreensAsync(JsonElement args, CancellationToken ct)
    {
        var projectId = RequiredString(args, "projectId");
        if (await _projects.GetAsync(projectId, ct) is null) throw new InvalidOperationException("Project not found.");
        return await _screens.ListAsync(projectId, ct);
    }

    private async Task<object> InstallModuleAsync(JsonElement args, CancellationToken ct)
    {
        var projectId = RequiredString(args, "projectId");
        var moduleId = RequiredString(args, "moduleId");
        var version = RequiredString(args, "version");
        return await _moduleInstaller.InstallAsync(projectId, moduleId, version, ct);
    }

    private async Task<object> UpgradeModuleAsync(JsonElement args, CancellationToken ct)
    {
        var projectId = RequiredString(args, "projectId");
        var moduleId = RequiredString(args, "moduleId");
        var version = RequiredString(args, "version");
        return await _moduleInstaller.UpgradeAsync(projectId, moduleId, version, ct);
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

    private async Task<object> PreviewProjectAsync(JsonElement args, CancellationToken ct)
    {
        var id = RequiredString(args, "projectId");
        if (await _projects.GetAsync(id, ct) is null) throw new InvalidOperationException("Project not found.");
        var rendered = new List<object>();
        foreach (var placement in await _placements.ListAsync(id, ct))
        {
            var definition = await _components.GetAsync(placement.ComponentAlias, placement.Version, ct)
                ?? throw new InvalidOperationException($"Component '{placement.ComponentAlias}' is not installed.");
            rendered.Add(new { placement.Id, componentAlias = placement.ComponentAlias,
                html = SiteKitComponentRenderer.Render(definition, placement.Data, placement.Variant, placement.StyleClass, placement.Appearance) });
        }
        return new { projectId = id, components = rendered };
    }

    private async Task<object> ValidateProjectAsync(JsonElement args, CancellationToken ct)
    {
        var id = RequiredString(args, "projectId");
        var project = await _projects.GetAsync(id, ct) ?? throw new InvalidOperationException("Project not found.");
        var errors = new List<string>();
        foreach (var alias in project.Components)
            if (await _components.GetAsync(alias, cancellationToken: ct) is null) errors.Add($"Component '{alias}' is not installed.");
        foreach (var alias in project.Entities)
            if (await _entities.GetAsync(id, alias, ct) is null) errors.Add($"Entity '{alias}' is missing its definition.");
        foreach (var alias in project.Screens)
        {
            var screen = await _screens.GetAsync(id, alias, ct);
            if (screen is null) { errors.Add($"Screen '{alias}' is missing its definition."); continue; }
            var entity = screen.EntityAlias is null ? null : await _entities.GetAsync(id, screen.EntityAlias, ct);
            if (screen.EntityAlias is not null && entity is null) errors.Add($"Screen '{alias}' references missing entity '{screen.EntityAlias}'.");
            errors.AddRange(SiteKitScreenValidator.Validate(screen, entity).Select(error => $"Screen '{alias}': {error}"));
            foreach (var component in screen.Components)
                if (await _components.GetAsync(component, cancellationToken: ct) is null) errors.Add($"Screen '{alias}' references missing component '{component}'.");
        }
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
        var generatedProject = Path.Combine(_root, "generated-projects", id, id + ".csproj");
        var solution = Path.Combine(_root, "UmbraccoEnv.sln");
        var target = File.Exists(generatedProject) ? generatedProject : solution;
        if (!File.Exists(target)) throw new InvalidOperationException("Neither the generated project nor UmbraccoEnv.sln was found under the configured SiteKit root.");
        var dotnet = Environment.GetEnvironmentVariable("SITEKIT_DOTNET_EXECUTABLE");
        var generatedBuild = File.Exists(generatedProject);
        var start = new ProcessStartInfo(string.IsNullOrWhiteSpace(dotnet) ? "dotnet" : Path.GetFullPath(dotnet))
        {
            WorkingDirectory = generatedBuild ? Path.GetDirectoryName(generatedProject)! : _root,
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        start.ArgumentList.Add("build");
        start.ArgumentList.Add(target);
        // Generated projects have their own first-run dependency restore; the
        // checked-in solution can keep the fast, reproducible no-restore build.
        if (!generatedBuild) start.ArgumentList.Add("--no-restore");
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
        return new { projectId = id, target, exitCode = process.ExitCode, outputTail = tail };
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
