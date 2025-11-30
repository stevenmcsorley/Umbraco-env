using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Services;
using Umbraco.Cms.Core.Services;

namespace MyDockerProject.Controllers;

public class AccountController : Controller
{
    private readonly Services.IUserService _userService;
    private readonly IContentService _contentService;

    public AccountController(Services.IUserService userService, IContentService contentService)
    {
        _userService = userService;
        _contentService = contentService;
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
        
        // Fetch product (room/event) and hotel details for each booking
        var bookingsWithDetails = new List<dynamic>();
        foreach (var booking in bookings)
        {
            string? productName = null;
            string? hotelName = null;
            string? hotelLocation = null;
            
            try
            {
                var productContent = _contentService.GetById(booking.ProductId);
                if (productContent != null)
                {
                    productName = productContent.GetValue<string>("roomName") 
                        ?? productContent.GetValue<string>("eventName") 
                        ?? productContent.Name;
                    
                    // Get hotel (parent of room/event)
                    if (productContent.ParentId > 0)
                    {
                        var hotelContent = _contentService.GetById(productContent.ParentId);
                        if (hotelContent != null)
                        {
                            hotelName = hotelContent.GetValue<string>("hotelName") ?? hotelContent.Name;
                            hotelLocation = hotelContent.GetValue<string>("location");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue
                Console.WriteLine($"[AccountController] Error fetching details for booking {booking.Id}: {ex.Message}");
            }
            
            bookingsWithDetails.Add(new
            {
                Booking = booking,
                ProductName = productName,
                HotelName = hotelName,
                HotelLocation = hotelLocation
            });
        }
        
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
        ViewBag.Bookings = bookingsWithDetails;
        return View();
    }
}

