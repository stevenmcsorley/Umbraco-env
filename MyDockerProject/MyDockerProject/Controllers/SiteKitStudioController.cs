using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Security;

namespace MyDockerProject.Controllers;

[TypeFilter(typeof(SiteKitAdminAuthorizationFilter))]
public sealed class SiteKitStudioController : Controller
{
    [HttpGet("/studio/components")]
    public IActionResult Components() => View("Components");

    [HttpGet("/studio/projects")]
    public IActionResult Projects() => View("Projects");

    [HttpGet("/studio/screens")]
    public IActionResult Screens() => View("Screens");

    [HttpGet("/studio/modules")]
    public IActionResult Modules() => View("Modules");

    [HttpGet("/studio/imports")]
    public IActionResult Imports() => View("Imports");
}
