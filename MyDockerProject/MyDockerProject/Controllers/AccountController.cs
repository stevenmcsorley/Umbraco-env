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
        
        // Debug logging
        Console.WriteLine($"[AccountController] UserId: {userId}, Bookings count: {bookings?.Count ?? 0}");
        if (bookings != null && bookings.Count > 0)
        {
            foreach (var booking in bookings)
            {
                Console.WriteLine($"[AccountController] Booking - Id: {booking.Id}, UserId: {booking.UserId?.ToString() ?? "NULL"}, Reference: {booking.BookingReference}");
            }
        }

        ViewBag.User = user;
        ViewBag.Bookings = bookings;
        return View();
    }
}

