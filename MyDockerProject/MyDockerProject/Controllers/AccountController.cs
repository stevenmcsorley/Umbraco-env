using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Services;

namespace MyDockerProject.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;

    public AccountController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("/account")]
    public async Task<IActionResult> Dashboard()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        var bookings = await _userService.GetUserBookingsAsync(userId);

        ViewBag.User = user;
        return View(bookings);
    }
}

