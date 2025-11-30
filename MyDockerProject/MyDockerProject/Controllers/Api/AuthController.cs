using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Models;
using MyDockerProject.Services;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        var user = await _userService.RegisterAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Phone
        );

        if (user == null)
        {
            return BadRequest(new { error = "Email already registered" });
        }

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        var user = await _userService.LoginAsync(request.Email, request.Password);

        if (user == null)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            token = GenerateSimpleToken(user.Id) // In production, use JWT or similar
        });
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            phone = user.Phone,
            createdAt = user.CreatedAt,
            lastLoginAt = user.LastLoginAt
        });
    }

    [HttpGet("user/{userId}/bookings")]
    public async Task<IActionResult> GetUserBookings(Guid userId)
    {
        var bookings = await _userService.GetUserBookingsAsync(userId);
        
        return Ok(bookings.Select(b => new
        {
            bookingId = b.Id,
            bookingReference = b.BookingReference,
            productId = b.ProductId,
            productType = b.ProductType,
            checkIn = b.CheckIn,
            checkOut = b.CheckOut,
            quantity = b.Quantity,
            totalPrice = b.TotalPrice,
            currency = b.Currency,
            status = b.Status,
            createdAt = b.CreatedAt
        }));
    }

    private string GenerateSimpleToken(Guid userId)
    {
        // Simple token generation - in production, use JWT or similar
        var tokenData = $"{userId}:{DateTime.UtcNow:O}";
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenData);
        return Convert.ToBase64String(tokenBytes);
    }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

