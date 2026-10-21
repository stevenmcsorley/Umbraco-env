using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Security;
using SiteKit.Core.Components;
using SiteKit.Core.Projects;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/sitekit/projects/{projectId}/placements")]
[TypeFilter(typeof(SiteKitAdminAuthorizationFilter))]
public sealed class SiteKitPlacementsController : ControllerBase
{
    private readonly ISiteKitPlacementRegistry _placements;
    private readonly ISiteKitProjectRegistry _projects;
    private readonly ISiteKitComponentRegistry _components;

    public SiteKitPlacementsController(ISiteKitPlacementRegistry placements, ISiteKitProjectRegistry projects, ISiteKitComponentRegistry components)
    {
        _placements = placements;
        _projects = projects;
        _components = components;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteKitComponentPlacement>>> List(string projectId, CancellationToken cancellationToken)
    {
        if (!SiteKitProjectValidator.IsValidId(projectId)) return BadRequest(new { error = "Project id is invalid." });
        if (await _projects.GetAsync(projectId, cancellationToken) is null) return NotFound(new { error = "Project not found." });
        return Ok(await _placements.ListAsync(projectId, cancellationToken));
    }

    [HttpPut]
    [RequestSizeLimit(1_048_576)]
    public async Task<ActionResult<SiteKitComponentPlacement>> Save(string projectId, [FromBody] SiteKitComponentPlacement placement, CancellationToken cancellationToken)
    {
        if (!SiteKitProjectValidator.IsValidId(projectId)) return BadRequest(new { error = "Project id is invalid." });
        if (placement is null) return BadRequest(new { error = "Placement is required." });
        if (await _projects.GetAsync(projectId, cancellationToken) is null) return NotFound(new { error = "Project not found." });
        var errors = JsonSiteKitPlacementRegistry.ValidatePlacement(placement).ToList();
        var definition = await _components.GetAsync(placement.ComponentAlias, placement.Version, cancellationToken);
        if (definition is null) errors.Add("The referenced component version was not found.");
        else
        {
            errors.AddRange(SiteKitComponentValidator.ValidateData(definition, placement.Data));
            if (placement.Variant is not null && definition.Variants.All(x => x.Alias != placement.Variant)) errors.Add("The referenced variant was not found.");
        }
        if (errors.Count > 0) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["placement"] = errors.ToArray() }));
        return Ok(await _placements.SaveAsync(projectId, placement, cancellationToken));
    }

    [HttpDelete("{placementId}")]
    public async Task<IActionResult> Delete(string projectId, string placementId, CancellationToken cancellationToken)
    {
        if (!SiteKitProjectValidator.IsValidId(projectId)) return BadRequest(new { error = "Project id is invalid." });
        if (await _projects.GetAsync(projectId, cancellationToken) is null) return NotFound();
        try { return await _placements.DeleteAsync(projectId, placementId, cancellationToken) ? NoContent() : NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{placementId}/preview")]
    public async Task<IActionResult> Preview(string projectId, string placementId, CancellationToken cancellationToken)
    {
        if (!SiteKitProjectValidator.IsValidId(projectId)) return BadRequest(new { error = "Project id is invalid." });
        if (await _projects.GetAsync(projectId, cancellationToken) is null) return NotFound();
        var placement = await _placements.GetAsync(projectId, placementId, cancellationToken);
        if (placement is null) return NotFound();
        var definition = await _components.GetAsync(placement.ComponentAlias, placement.Version, cancellationToken);
        if (definition is null) return Conflict(new { error = "The shared component version is unavailable." });
        try { return Content(SiteKitComponentRenderer.Render(definition, placement.Data, placement.Variant, placement.StyleClass, placement.Appearance), "text/html"); }
        catch (ArgumentException exception) { return Conflict(new { error = exception.Message }); }
    }
}
