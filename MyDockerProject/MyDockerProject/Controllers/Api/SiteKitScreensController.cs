using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Security;
using SiteKit.Core.Components;
using SiteKit.Core.Data;
using SiteKit.Core.Projects;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/sitekit/projects/{projectId}/screens")]
[TypeFilter(typeof(SiteKitAdminAuthorizationFilter))]
public sealed class SiteKitScreensController : ControllerBase
{
    private readonly ISiteKitScreenRegistry _screens;
    private readonly ISiteKitProjectRegistry _projects;
    private readonly ISiteKitEntityRegistry _entities;
    private readonly ISiteKitComponentRegistry _components;
    private readonly ISiteKitRecordStore _records;
    private readonly ISiteKitPermissionRegistry _permissions;

    public SiteKitScreensController(ISiteKitScreenRegistry screens, ISiteKitProjectRegistry projects, ISiteKitEntityRegistry entities,
        ISiteKitComponentRegistry components, ISiteKitRecordStore records, ISiteKitPermissionRegistry permissions)
    { _screens = screens; _projects = projects; _entities = entities; _components = components; _records = records; _permissions = permissions; }

    [HttpGet]
    public async Task<IActionResult> List(string projectId, CancellationToken ct)
        => await ProjectExists(projectId, ct) ? Ok(await _screens.ListAsync(projectId, ct)) : NotFound();

    [HttpGet("{alias}")]
    public async Task<IActionResult> Get(string projectId, string alias, CancellationToken ct)
    {
        if (!await ProjectExists(projectId, ct)) return NotFound();
        var screen = await _screens.GetAsync(projectId, alias, ct);
        return screen is null ? NotFound() : Ok(screen);
    }

    [HttpPut("{alias}")]
    [RequestSizeLimit(1_048_576)]
    public async Task<IActionResult> Save(string projectId, string alias, [FromBody] SiteKitApplicationScreenDefinition screen, CancellationToken ct)
    {
        if (!await ProjectExists(projectId, ct)) return NotFound();
        if (screen.ProjectId != projectId || screen.Alias != alias) return BadRequest(new { error = "Route and screen identifiers must match." });
        SiteKitEntityDefinition? entity = null;
        if (screen.EntityAlias is not null)
        {
            entity = await _entities.GetAsync(projectId, screen.EntityAlias, ct);
            if (entity is null) return BadRequest(new { error = $"Entity '{screen.EntityAlias}' is not installed in this project." });
        }
        var errors = SiteKitScreenValidator.Validate(screen, entity).ToList();
        foreach (var component in screen.Components ?? [])
            if (await _components.GetAsync(component, cancellationToken: ct) is null) errors.Add($"Component '{component}' is not installed.");
        if (errors.Count > 0) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["screen"] = errors.ToArray() }));
        try
        {
            var saved = await _screens.SaveAsync(screen, ct);
            var project = await _projects.GetAsync(projectId, ct);
            if (project is not null && !project.Screens.Contains(alias, StringComparer.Ordinal))
                await _projects.SaveAsync(project with { Screens = project.Screens.Append(alias).ToArray() }, ct);
            return Ok(saved);
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{alias}/preview")]
    public async Task<IActionResult> Preview(string projectId, string alias, CancellationToken ct)
    {
        var screen = await _screens.GetAsync(projectId, alias, ct);
        if (screen is null) return NotFound();
        SiteKitEntityDefinition? entity = screen.EntityAlias is null ? null : await _entities.GetAsync(projectId, screen.EntityAlias, ct);
        if (screen.EntityAlias is not null && entity is null) return Conflict(new { error = "Screen entity is missing." });
        if (entity is not null)
        {
            var principal = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrWhiteSpace(principal)) return Unauthorized();
            var grant = await _permissions.GetAsync(projectId, entity.Alias, principal, ct);
            if (grant?.Operations.Contains("read", StringComparer.Ordinal) != true && grant?.Operations.Contains("manage", StringComparer.Ordinal) != true)
                return Forbid();
        }
        var schemaErrors = SiteKitScreenValidator.Validate(screen, entity);
        if (schemaErrors.Count > 0) return Conflict(new { errors = schemaErrors });
        var records = entity is null ? [] : await _records.ListAsync(projectId, entity.Alias, ct);
        var fields = screen.Columns.Concat(screen.FormFields).Distinct(StringComparer.Ordinal)
            .Select(alias => entity?.Fields.FirstOrDefault(field => field.Alias == alias))
            .Where(field => field is not null).ToArray();
        return Ok(new { screen, entity, fields, records });
    }

    private async Task<bool> ProjectExists(string projectId, CancellationToken ct)
        => SiteKitProjectValidator.IsValidId(projectId) && await _projects.GetAsync(projectId, ct) is not null;
}
