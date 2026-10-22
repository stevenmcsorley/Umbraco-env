using Microsoft.AspNetCore.Mvc;

namespace MyDockerProject.Controllers;

[Route("booking/manage")]
public sealed class BookingManagementController : Controller
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Index()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return View("~/Views/BookingManagement/Index.cshtml");
    }
}
