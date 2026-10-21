using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Security;
using SiteKit.Core.Components;
using SiteKit.Core.Modules;
using SiteKit.Core.Projects;
using SiteKit.Core.Data;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/sitekit/modules")]
[TypeFilter(typeof(SiteKitAdminAuthorizationFilter))]
public sealed class SiteKitModulesController : ControllerBase
{
    private readonly ISiteKitModuleRegistry _modules;
    private readonly ISiteKitProjectRegistry _projects;
    private readonly ISiteKitComponentRegistry _components;
    private readonly ISiteKitEntityRegistry _entities;
    private readonly IWebHostEnvironment _environment;

    public SiteKitModulesController(ISiteKitModuleRegistry modules, ISiteKitProjectRegistry projects, ISiteKitComponentRegistry components, ISiteKitEntityRegistry entities, IWebHostEnvironment environment)
    { _modules = modules; _projects = projects; _components = components; _entities = entities; _environment = environment; }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _modules.ListAsync(ct));

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length is <= 0 or > 10 * 1024 * 1024) return BadRequest(new { error = "Select a module ZIP no larger than 10 MiB." });
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await _modules.ImportAsync(stream, ct));
        }
        catch (InvalidDataException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }

    [HttpGet("{id}/{version}/export")]
    public async Task<IActionResult> Export(string id, string version, CancellationToken ct)
    {
        try { return File(await _modules.ExportAsync(id, version, ct), "application/zip", $"{id}-{version}.sitekit-module.zip"); }
        catch (FileNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("{id}/{version}/install/{projectId}")]
    public async Task<IActionResult> Install(string id, string version, string projectId, CancellationToken ct)
    {
        var installer = new SiteKitModuleInstaller(Path.Combine(_environment.ContentRootPath, "umbraco", "Data", "SiteKit", "modules"), _modules, _projects, _components, _entities);
        try { return Ok(await installer.InstallAsync(projectId, id, version, ct)); }
        catch (FileNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { error = exception.Message }); }
    }
}
