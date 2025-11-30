using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDockerProject.Data;
using MyDockerProject.Models;
using MyDockerProject.Services;

namespace MyDockerProject.Controllers;

public class AdminController : Controller
{
    private readonly BookingDbContext _context;
    private readonly BookingService _bookingService;
    private readonly InventoryService _inventoryService;

    public AdminController(
        BookingDbContext context,
        BookingService bookingService,
        InventoryService inventoryService)
    {
        _context = context;
        _bookingService = bookingService;
        _inventoryService = inventoryService;
    }

    [HttpGet("/admin")]
    public async Task<IActionResult> Dashboard()
    {
        var bookings = await _context.Bookings
            .OrderByDescending(b => b.CreatedAt)
            .Take(50)
            .ToListAsync();

        var totalBookings = await _context.Bookings.CountAsync();
        var totalRevenue = await _context.Bookings
            .Where(b => b.PaymentStatus == "Paid")
            .SumAsync(b => b.TotalPrice);
        var pendingBookings = await _context.Bookings
            .CountAsync(b => b.Status == "Confirmed" && b.PaymentStatus != "Paid");
        var cancelledBookings = await _context.Bookings
            .CountAsync(b => b.Status == "Cancelled");

        var inventorySummary = await _context.Inventory
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalEntries = g.Count(),
                TotalAvailable = g.Sum(i => i.AvailableQuantity),
                TotalBooked = g.Sum(i => i.BookedQuantity)
            })
            .ToListAsync();

        ViewBag.TotalBookings = totalBookings;
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.PendingBookings = pendingBookings;
        ViewBag.CancelledBookings = cancelledBookings;
        ViewBag.InventorySummary = inventorySummary;

        return View(bookings);
    }

    [HttpGet("/admin/bookings")]
    public async Task<IActionResult> Bookings(string? status = null, string? search = null)
    {
        var query = _context.Bookings.AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(b => b.Status == status);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(b => 
                b.BookingReference.Contains(search) ||
                b.GuestEmail.Contains(search) ||
                b.GuestFirstName.Contains(search) ||
                b.GuestLastName.Contains(search));
        }

        var bookings = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }

    [HttpGet("/admin/inventory")]
    public async Task<IActionResult> Inventory(Guid? productId = null)
    {
        var query = _context.Inventory.AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(i => i.ProductId == productId.Value);
        }

        var inventory = await query
            .OrderBy(i => i.Date)
            .ToListAsync();

        return View(inventory);
    }

    [HttpPost("/admin/bookings/{bookingReference}/cancel")]
    public async Task<IActionResult> CancelBooking(string bookingReference)
    {
        try
        {
            await _bookingService.CancelBookingAsync(bookingReference);
            TempData["Success"] = $"Booking {bookingReference} cancelled successfully";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error cancelling booking: {ex.Message}";
        }

        return RedirectToAction("Bookings");
    }
}

