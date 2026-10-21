using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Security;
using SiteKit.Core.Components;
using System.Text.Json;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/sitekit/components")]
[TypeFilter(typeof(SiteKitAdminAuthorizationFilter))]
public sealed class SiteKitComponentsController : ControllerBase
{
    private readonly ISiteKitComponentRegistry _registry;

    public SiteKitComponentsController(ISiteKitComponentRegistry registry) => _registry = registry;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteKitComponentDefinition>>> List(CancellationToken cancellationToken)
        => Ok(await _registry.ListAsync(cancellationToken));

    [HttpGet("{alias}")]
    public async Task<ActionResult<SiteKitComponentDefinition>> Get(string alias, [FromQuery] string? version, CancellationToken cancellationToken)
    {
        try
        {
            var definition = await _registry.GetAsync(alias, version, cancellationToken);
            return definition is null ? NotFound() : Ok(definition);
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost]
    [RequestSizeLimit(1_048_576)]
    public async Task<ActionResult<SiteKitComponentDefinition>> Save([FromBody] SiteKitComponentDefinition definition, CancellationToken cancellationToken)
    {
        var errors = SiteKitComponentValidator.ValidateDefinition(definition);
        if (errors.Count > 0) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["definition"] = errors.ToArray() }));
        var saved = await _registry.SaveAsync(definition, cancellationToken);
        return CreatedAtAction(nameof(Get), new { alias = saved.Alias, version = saved.Version }, saved);
    }

    [HttpDelete("{alias}/{version}")]
    public async Task<IActionResult> Delete(string alias, string version, CancellationToken cancellationToken)
    {
        try { return await _registry.DeleteAsync(alias, version, cancellationToken) ? NoContent() : NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{alias}/{version}/export")]
    public async Task<IActionResult> Export(string alias, string version, CancellationToken cancellationToken)
    {
        try
        {
            var package = await _registry.ExportAsync(alias, version, cancellationToken);
            return File(package, "application/json", $"{alias}-{version}.sitekit-component.json");
        }
        catch (FileNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("import")]
    [RequestSizeLimit(1_048_576)]
    public async Task<ActionResult<SiteKitComponentDefinition>> Import(CancellationToken cancellationToken)
    {
        try
        {
            var imported = await _registry.ImportAsync(Request.Body, cancellationToken);
            return Ok(imported);
        }
        catch (InvalidDataException exception) { return BadRequest(new { error = exception.Message }); }
        catch (System.Text.Json.JsonException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("validate-data")]
    public async Task<IActionResult> ValidateData([FromBody] ValidateComponentDataRequest request, CancellationToken cancellationToken)
    {
        var definition = await _registry.GetAsync(request.Alias, request.Version, cancellationToken);
        if (definition is null) return NotFound();
        var errors = SiteKitComponentValidator.ValidateData(definition, request.Data);
        return errors.Count == 0 ? NoContent() : BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["data"] = errors.ToArray() }));
    }

    [HttpPost("preview")]
    [RequestSizeLimit(1_048_576)]
    public async Task<IActionResult> Preview([FromBody] PreviewComponentRequest request, CancellationToken cancellationToken)
    {
        var definition = request.Definition ?? await _registry.GetAsync(request.Alias, request.Version, cancellationToken);
        if (definition is null) return NotFound();
        try { return Content(SiteKitComponentRenderer.Render(definition, request.Data, request.Variant), "text/html"); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    public sealed record ValidateComponentDataRequest(string Alias, JsonElement Data, string? Version = null);
    public sealed record PreviewComponentRequest(string Alias, JsonElement Data, string? Version = null, string? Variant = null, SiteKitComponentDefinition? Definition = null);
}
