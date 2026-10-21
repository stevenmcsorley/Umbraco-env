using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Security;
using SiteKit.Core.Components;
using SiteKit.Core.Projects;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/sitekit/projects")]
[TypeFilter(typeof(SiteKitAdminAuthorizationFilter))]
public sealed class SiteKitProjectsController : ControllerBase
{
    private readonly ISiteKitProjectRegistry _registry;
    public SiteKitProjectsController(ISiteKitProjectRegistry registry) => _registry = registry;

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
}
