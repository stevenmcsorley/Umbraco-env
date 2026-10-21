using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Security;
using SiteKit.Core.Data;
using SiteKit.Core.Projects;
using Umbraco.Cms.Core.Events;
using MyDockerProject.Services.SiteKitAutomate;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/sitekit/projects/{projectId}/entities")]
[TypeFilter(typeof(SiteKitAdminAuthorizationFilter))]
public sealed class SiteKitEntitiesController : ControllerBase
{
    private readonly ISiteKitEntityRegistry _entities;
    private readonly ISiteKitProjectRegistry _projects;
    private readonly ISiteKitRecordStore _records;
    private readonly ISiteKitPermissionRegistry _permissions;
    private readonly IConfiguration _configuration;
    private readonly IEventAggregator _events;

    public SiteKitEntitiesController(ISiteKitEntityRegistry entities, ISiteKitProjectRegistry projects, ISiteKitRecordStore records, ISiteKitPermissionRegistry permissions, IConfiguration configuration, IEventAggregator events)
    { _entities = entities; _projects = projects; _records = records; _permissions = permissions; _configuration = configuration; _events = events; }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteKitEntityDefinition>>> List(string projectId, CancellationToken ct)
    {
        if (!await ProjectExists(projectId, ct)) return NotFound();
        return Ok(await _entities.ListAsync(projectId, ct));
    }

    [HttpGet("{alias}")]
    public async Task<ActionResult<SiteKitEntityDefinition>> Get(string projectId, string alias, CancellationToken ct)
    {
        if (!await ProjectExists(projectId, ct)) return NotFound();
        var entity = await _entities.GetAsync(projectId, alias, ct);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPut("{alias}")]
    [RequestSizeLimit(1_048_576)]
    public async Task<ActionResult<SiteKitEntityDefinition>> Save(string projectId, string alias, [FromBody] SiteKitEntityDefinition definition, CancellationToken ct)
    {
        if (!await ProjectExists(projectId, ct)) return NotFound();
        if (definition.ProjectId != projectId || definition.Alias != alias) return BadRequest(new { error = "Route and definition identifiers must match." });
        var errors = SiteKitEntityValidator.Validate(definition);
        foreach (var field in definition.Fields.Where(field => field.Type is "relationship" or "relationships" && SiteKitProjectValidator.IsValidId(field.RelatedEntity)))
            if (field.RelatedEntity != definition.Alias && await _entities.GetAsync(projectId, field.RelatedEntity!, ct) is null) errors = errors.Append($"Related entity '{field.RelatedEntity}' is not defined.").ToArray();
        if (errors.Count > 0) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["definition"] = errors.ToArray() }));
        try
        {
            var saved = await _entities.SaveAsync(definition, ct);
            var project = await _projects.GetAsync(projectId, ct);
            if (project is not null && !project.Entities.Contains(alias, StringComparer.Ordinal))
                await _projects.SaveAsync(project with { Entities = project.Entities.Append(alias).ToArray() }, ct);
            return Ok(saved);
        }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }

    [HttpPost("{alias}/validate")]
    public async Task<IActionResult> Validate(string projectId, string alias, [FromBody] JsonElement data, CancellationToken ct)
    {
        var entity = await _entities.GetAsync(projectId, alias, ct);
        if (entity is null) return NotFound();
        var errors = SiteKitEntityValidator.ValidateRecord(entity, data);
        return errors.Count == 0 ? NoContent() : BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["record"] = errors.ToArray() }));
    }

    [HttpGet("{alias}/permissions")]
    public async Task<IActionResult> ListPermissions(string projectId, string alias, CancellationToken ct)
    {
        if (await _entities.GetAsync(projectId, alias, ct) is null) return NotFound();
        return Ok(await _permissions.ListAsync(projectId, alias, ct));
    }

    [HttpPut("{alias}/permissions")]
    public async Task<IActionResult> SavePermission(string projectId, string alias, [FromBody] SiteKitEntityPermissionGrant grant, CancellationToken ct)
    {
        if (await _entities.GetAsync(projectId, alias, ct) is null) return NotFound();
        var email = HttpContext.Session.GetString("UserEmail");
        var admins = (_configuration["SiteKit:AdminEmails"] ?? string.Empty).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (string.IsNullOrWhiteSpace(email) || !admins.Contains(email, StringComparer.OrdinalIgnoreCase)) return Forbid();
        if (grant.ProjectId != projectId || grant.EntityAlias != alias) return BadRequest(new { error = "Route and grant identifiers must match." });
        try { return Ok(await _permissions.SaveAsync(grant, ct)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{alias}/records")]
    public async Task<IActionResult> ListRecords(string projectId, string alias, CancellationToken ct)
    {
        var entity = await _entities.GetAsync(projectId, alias, ct);
        if (entity is null) return NotFound();
        if (!await Can("read", projectId, alias, ct)) return Forbid();
        return Ok(await _records.ListAsync(projectId, alias, ct));
    }

    [HttpGet("{alias}/records/{id}")]
    public async Task<IActionResult> GetRecord(string projectId, string alias, string id, CancellationToken ct)
    {
        var entity = await _entities.GetAsync(projectId, alias, ct);
        if (entity is null) return NotFound();
        if (!await Can("read", projectId, alias, ct)) return Forbid();
        var record = await _records.GetAsync(projectId, alias, id, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("{alias}/records")]
    [RequestSizeLimit(1_048_576)]
    public Task<IActionResult> CreateRecord(string projectId, string alias, [FromBody] JsonElement data, CancellationToken ct)
        => SaveRecord(projectId, alias, data, null, "create", ct);

    [HttpPut("{alias}/records/{id}")]
    [RequestSizeLimit(1_048_576)]
    public Task<IActionResult> UpdateRecord(string projectId, string alias, string id, [FromBody] JsonElement data, CancellationToken ct)
        => SaveRecord(projectId, alias, data, id, "update", ct);

    [HttpDelete("{alias}/records/{id}")]
    public async Task<IActionResult> DeleteRecord(string projectId, string alias, string id, CancellationToken ct)
    {
        var entity = await _entities.GetAsync(projectId, alias, ct);
        if (entity is null) return NotFound();
        if (!await Can("delete", projectId, alias, ct)) return Forbid();
        if (!await _records.DeleteAsync(projectId, alias, id, HttpContext.Session.GetString("UserEmail")!, await _entities.ListAsync(projectId, ct), ct)) return NotFound();
        _events.Publish(new SiteKitOperationalRecordChangedNotification(projectId, alias, id, "deleted", DateTimeOffset.UtcNow));
        return NoContent();
    }

    private async Task<IActionResult> SaveRecord(string projectId, string alias, JsonElement data, string? id, string permission, CancellationToken ct)
    {
        var entity = await _entities.GetAsync(projectId, alias, ct);
        if (entity is null) return NotFound();
        if (!await Can(permission, projectId, alias, ct)) return Forbid();
        try
        {
            var relationshipErrors = await ValidateRelationships(entity, data, ct);
            if (relationshipErrors.Count > 0) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["relationships"] = relationshipErrors.ToArray() }));
            var record = await _records.SaveAsync(entity, data, HttpContext.Session.GetString("UserEmail")!, id, ct);
            _events.Publish(new SiteKitOperationalRecordChangedNotification(projectId, alias, record.Id, id is null ? "created" : "updated", record.UpdatedAt));
            return id is null ? CreatedAtAction(nameof(GetRecord), new { projectId, alias, id = record.Id }, record) : Ok(record);
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }

    private async Task<IReadOnlyList<string>> ValidateRelationships(SiteKitEntityDefinition source, JsonElement data, CancellationToken ct)
    {
        var errors = new List<string>();
        foreach (var field in source.Fields.Where(field => field.Type is "relationship" or "relationships" && data.TryGetProperty(field.Alias, out _)))
        {
            var targetAlias = field.RelatedEntity!;
            if (await _entities.GetAsync(source.ProjectId, targetAlias, ct) is null)
            { errors.Add($"Related entity '{targetAlias}' is not defined."); continue; }
            var ids = field.Type == "relationships"
                ? data.GetProperty(field.Alias).EnumerateArray().Select(value => value.GetString()!).ToArray()
                : [data.GetProperty(field.Alias).GetString()!];
            foreach (var relatedId in ids)
                if (await _records.GetAsync(source.ProjectId, targetAlias, relatedId, ct) is null)
                    errors.Add($"Field '{field.Alias}' references a missing {targetAlias} record.");
        }
        return errors;
    }

    private async Task<bool> Can(string operation, string projectId, string alias, CancellationToken ct)
    {
        var principal = HttpContext.Session.GetString("UserEmail");
        if (string.IsNullOrWhiteSpace(principal)) return false;
        var grant = await _permissions.GetAsync(projectId, alias, principal, ct);
        return grant?.Operations.Contains(operation, StringComparer.Ordinal) == true || grant?.Operations.Contains("manage", StringComparer.Ordinal) == true;
    }

    private async Task<bool> ProjectExists(string projectId, CancellationToken ct)
        => SiteKitProjectValidator.IsValidId(projectId) && await _projects.GetAsync(projectId, ct) is not null;
}
