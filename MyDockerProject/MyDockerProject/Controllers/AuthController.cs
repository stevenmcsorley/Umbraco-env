using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Services;
using MyDockerProject.Security;

namespace MyDockerProject.Controllers;

public class AuthController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(IUserService userService, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _userService = userService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("/register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string email, string password, string firstName, string lastName, string? phone = null, string? adminRegistrationCode = null)
    {
        email = email?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Email and password are required";
            return View();
        }

        if (!SiteKitAdminRegistrationGuard.CanRegister(email, adminRegistrationCode, _configuration))
        {
            ViewBag.Error = "Administrator accounts require the deployment's separate one-time setup code.";
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
    [ValidateAntiForgeryToken]
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

