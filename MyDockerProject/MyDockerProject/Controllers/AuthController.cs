using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Services;

namespace MyDockerProject.Controllers;

public class AuthController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("/register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("/register")]
    public async Task<IActionResult> Register(string email, string password, string firstName, string lastName, string? phone = null)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Email and password are required";
            return View();
        }

        var user = await _userService.RegisterAsync(email, password, firstName, lastName, phone);

        if (user == null)
        {
            ViewBag.Error = "Email already registered";
            return View();
        }

        TempData["Success"] = "Registration successful! You can now login.";
        return Redirect("/login");
    }

    [HttpGet("/login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("/login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Email and password are required";
            return View();
        }

        var user = await _userService.LoginAsync(email, password);

        if (user == null)
        {
            ViewBag.Error = "Invalid email or password";
            return View();
        }

        // Store user ID in session (simple implementation - in production use proper authentication)
        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");

        return Redirect("/account");
    }

    [HttpGet("/logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Redirect("/login");
    }
}

