using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Security;
using SiteKit.Core.Components;
using SiteKit.Core.Projects;
using SiteKit.Core.Data;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/sitekit/projects")]
[TypeFilter(typeof(SiteKitAdminAuthorizationFilter))]
public sealed class SiteKitProjectsController : ControllerBase
{
    private readonly ISiteKitProjectRegistry _registry;
    private readonly ISiteKitEntityRegistry _entities;
    private readonly ISiteKitScreenRegistry _screens;
    private readonly ISiteKitComponentRegistry _components;
    private readonly ISiteKitPlacementRegistry _placements;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    public SiteKitProjectsController(ISiteKitProjectRegistry registry, ISiteKitEntityRegistry entities, ISiteKitScreenRegistry screens,
        ISiteKitComponentRegistry components, ISiteKitPlacementRegistry placements, IWebHostEnvironment environment, IConfiguration configuration)
    { _registry = registry; _entities = entities; _screens = screens; _components = components; _placements = placements; _environment = environment; _configuration = configuration; }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteKitProjectManifest>>> List(CancellationToken cancellationToken)
        => Ok(await _registry.ListAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<SiteKitProjectManifest>> Get(string id, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _registry.GetAsync(id, cancellationToken);
            return project is null ? NotFound() : Ok(project);
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(262_144)]
    public async Task<ActionResult<SiteKitProjectManifest>> Save(string id, [FromBody] SiteKitProjectManifest manifest, CancellationToken cancellationToken)
    {
        if (!string.Equals(id, manifest.Id, StringComparison.Ordinal)) return BadRequest(new { error = "Route id must match project manifest id." });
        var errors = SiteKitProjectValidator.Validate(manifest);
        if (errors.Count != 0) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["project"] = errors.ToArray() }));
        return Ok(await _registry.SaveAsync(manifest, cancellationToken));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try { return await _registry.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("{id}/generate")]
    public async Task<IActionResult> Generate(string id, CancellationToken cancellationToken)
    {
        try
        {
            var generator = new SiteKitProjectGenerator(_configuration["SiteKit:GeneratedProjectsPath"] ?? Path.Combine(_environment.ContentRootPath, "generated-projects"), _registry, _screens, _entities);
            var directory = await generator.GenerateAsync(id, cancellationToken);
            return Ok(new { projectId = id, directory, buildCommand = $"dotnet build {id}.csproj", runCommand = $"dotnet run --project {id}.csproj" });
        }
        catch (InvalidOperationException exception) { return BadRequest(new { error = exception.Message }); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{id}/validate")]
    public async Task<IActionResult> Validate(string id, CancellationToken cancellationToken)
    {
        var project = await _registry.GetAsync(id, cancellationToken);
        if (project is null) return NotFound();
        var errors = new List<string>();
        foreach (var alias in project.Components)
            if (await _components.GetAsync(alias, cancellationToken: cancellationToken) is null) errors.Add($"Component '{alias}' is missing its definition.");
        foreach (var alias in project.Entities)
            if (await _entities.GetAsync(id, alias, cancellationToken) is null) errors.Add($"Entity '{alias}' is missing its definition.");
        foreach (var alias in project.Screens)
        {
            var screen = await _screens.GetAsync(id, alias, cancellationToken);
            if (screen is null) { errors.Add($"Screen '{alias}' is missing its definition."); continue; }
            var entity = screen.EntityAlias is null ? null : await _entities.GetAsync(id, screen.EntityAlias, cancellationToken);
            if (screen.EntityAlias is not null && entity is null) errors.Add($"Screen '{alias}' references missing entity '{screen.EntityAlias}'.");
            errors.AddRange(SiteKitScreenValidator.Validate(screen, entity).Select(error => $"Screen '{alias}': {error}"));
            foreach (var component in screen.Components)
                if (await _components.GetAsync(component, cancellationToken: cancellationToken) is null) errors.Add($"Screen '{alias}' references missing component '{component}'.");
        }
        foreach (var placement in await _placements.ListAsync(id, cancellationToken))
        {
            var component = await _components.GetAsync(placement.ComponentAlias, placement.Version, cancellationToken);
            if (component is null) errors.Add($"Placement '{placement.Id}' references a missing component.");
            else errors.AddRange(SiteKitComponentValidator.ValidateData(component, placement.Data).Select(error => $"Placement '{placement.Id}': {error}"));
        }
        return Ok(new { valid = errors.Count == 0, errors });
    }

}
