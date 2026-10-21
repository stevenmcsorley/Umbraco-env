using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Security;
using SiteKit.Core.Data;
using SiteKit.Core.Importing;
using SiteKit.Core.Projects;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/sitekit/projects/{projectId}/imports")]
[TypeFilter(typeof(SiteKitAdminAuthorizationFilter))]
public sealed class SiteKitImportsController : ControllerBase
{
    private readonly ISiteKitImportMappingRegistry _mappings;
    private readonly ISiteKitEntityRegistry _entities;
    private readonly ISiteKitPermissionRegistry _permissions;
    private readonly ISiteKitProjectRegistry _projects;
    private readonly SiteKitDataImportService _imports;
    private const long MaxUploadBytes = SiteKitDataImportService.MaximumBytes;

    public SiteKitImportsController(ISiteKitImportMappingRegistry mappings, ISiteKitEntityRegistry entities,
        ISiteKitPermissionRegistry permissions, ISiteKitProjectRegistry projects, SiteKitDataImportService imports)
    { _mappings = mappings; _entities = entities; _permissions = permissions; _projects = projects; _imports = imports; }

    [HttpGet]
    public async Task<IActionResult> List(string projectId, CancellationToken ct)
        => !await ProjectExists(projectId, ct) ? NotFound() : Ok(await _mappings.ListAsync(projectId, ct));

    [HttpPut("{alias}")]
    [RequestSizeLimit(262_144)]
    public async Task<IActionResult> Save(string projectId, string alias, [FromBody] SiteKitImportMapping mapping, CancellationToken ct)
    {
        if (!await ProjectExists(projectId, ct)) return NotFound();
        if (mapping.ProjectId != projectId || mapping.Alias != alias) return BadRequest(new { error = "Route and mapping identifiers must match." });
        var entity = await _entities.GetAsync(projectId, mapping.EntityAlias, ct);
        if (entity is null) return BadRequest(new { error = "Target entity is not defined in this project." });
        var invalid = mapping.Fields.Values.Where(target => target == "id" || !entity.Fields.Any(field => field.Alias == target)).ToArray();
        if (invalid.Length != 0) return BadRequest(new { error = "Mapping targets undefined or generated fields.", fields = invalid });
        try { return Ok(await _mappings.SaveAsync(mapping, ct)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("{alias}/preview")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> Preview(string projectId, string alias, IFormFile? file, CancellationToken ct)
    {
        var mapping = await GetMapping(projectId, alias, ct);
        if (mapping is null) return NotFound();
        if (file is null || file.Length is 0 or > MaxUploadBytes) return BadRequest(new { error = "Provide a file between 1 byte and 10 MiB." });
        if (!await Can("read", projectId, mapping.EntityAlias, ct)) return Forbid();
        try
        {
            var bytes = await ReadFile(file, ct);
            return Ok(await _imports.PreviewAsync(mapping, bytes, ct));
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }

    [HttpPost("{alias}/run")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> Run(string projectId, string alias, IFormFile? file, CancellationToken ct)
    {
        var mapping = await GetMapping(projectId, alias, ct);
        if (mapping is null) return NotFound();
        if (file is null || file.Length is 0 or > MaxUploadBytes) return BadRequest(new { error = "Provide a file between 1 byte and 10 MiB." });
        if (!await Can("import", projectId, mapping.EntityAlias, ct)) return Forbid();
        try
        {
            var bytes = await ReadFile(file, ct);
            return Ok(await _imports.ImportAsync(mapping, bytes, HttpContext.Session.GetString("UserEmail")!, ct));
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }

    private async Task<SiteKitImportMapping?> GetMapping(string projectId, string alias, CancellationToken ct)
    {
        if (!SiteKitProjectValidator.IsValidId(projectId) || !await ProjectExists(projectId, ct)) return null;
        return await _mappings.GetAsync(projectId, alias, ct);
    }

    private async Task<bool> Can(string operation, string projectId, string entityAlias, CancellationToken ct)
    {
        var principal = HttpContext.Session.GetString("UserEmail");
        if (string.IsNullOrWhiteSpace(principal)) return false;
        var grant = await _permissions.GetAsync(projectId, entityAlias, principal, ct);
        return grant?.Operations.Contains(operation, StringComparer.Ordinal) == true || grant?.Operations.Contains("manage", StringComparer.Ordinal) == true;
    }

    private async Task<bool> ProjectExists(string projectId, CancellationToken ct)
        => SiteKitProjectValidator.IsValidId(projectId) && await _projects.GetAsync(projectId, ct) is not null;

    private static async Task<ReadOnlyMemory<byte>> ReadFile(IFormFile file, CancellationToken ct)
    {
        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, ct);
        return stream.ToArray();
    }
}
