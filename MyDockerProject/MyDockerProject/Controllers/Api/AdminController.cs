using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDockerProject.Data;
using MyDockerProject.Models;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly BookingDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(BookingDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all inventory entries with optional filtering
    /// </summary>
    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory(
        [FromQuery] Guid? productId,
        [FromQuery] string? productType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        try
        {
            var query = _context.Inventory.AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(i => i.ProductId == productId.Value);
            }

            if (!string.IsNullOrEmpty(productType))
            {
                query = query.Where(i => i.ProductType == productType);
            }

            if (from.HasValue)
            {
                query = query.Where(i => i.Date >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(i => i.Date <= to.Value.Date);
            }

            var inventory = await query
                .OrderBy(i => i.ProductId)
                .ThenBy(i => i.Date)
                .ToListAsync();

            var result = inventory.Select(i => new
            {
                id = i.Id,
                productId = i.ProductId,
                productType = i.ProductType,
                date = i.Date.ToString("yyyy-MM-dd"),
                price = i.Price,
                currency = i.Currency,
                totalQuantity = i.TotalQuantity,
                bookedQuantity = i.BookedQuantity,
                availableQuantity = i.AvailableQuantity,
                isAvailable = i.IsAvailable,
                createdAt = i.CreatedAt,
                updatedAt = i.UpdatedAt
            }).ToList();

            return Ok(new
            {
                count = result.Count,
                inventory = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all bookings with optional filtering
    /// </summary>
    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings(
        [FromQuery] Guid? productId,
        [FromQuery] string? productType,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        try
        {
            var query = _context.Bookings.AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(b => b.ProductId == productId.Value);
            }

            if (!string.IsNullOrEmpty(productType))
            {
                query = query.Where(b => b.ProductType == productType);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (from.HasValue)
            {
                query = query.Where(b => b.CheckIn >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(b => b.CheckIn <= to.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var result = bookings.Select(b => new
            {
                id = b.Id,
                bookingReference = b.BookingReference,
                productId = b.ProductId,
                productType = b.ProductType,
                checkIn = b.CheckIn.ToString("yyyy-MM-dd"),
                checkOut = b.CheckOut?.ToString("yyyy-MM-dd"),
                quantity = b.Quantity,
                guestFirstName = b.GuestFirstName,
                guestLastName = b.GuestLastName,
                guestEmail = b.GuestEmail,
                guestPhone = b.GuestPhone,
                totalPrice = b.TotalPrice,
                currency = b.Currency,
                status = b.Status,
                createdAt = b.CreatedAt,
                updatedAt = b.UpdatedAt
            }).ToList();

            return Ok(new
            {
                count = result.Count,
                bookings = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bookings");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get inventory summary statistics
    /// </summary>
    [HttpGet("inventory/summary")]
    public async Task<IActionResult> GetInventorySummary()
    {
        try
        {
            var totalEntries = await _context.Inventory.CountAsync();
            var totalProducts = await _context.Inventory.Select(i => i.ProductId).Distinct().CountAsync();
            var totalBooked = await _context.Inventory.SumAsync(i => (int?)i.BookedQuantity) ?? 0;
            var totalAvailable = await _context.Inventory.SumAsync(i => (int?)(i.TotalQuantity - i.BookedQuantity)) ?? 0;
            var totalRevenue = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;

            var byProductType = await _context.Inventory
                .GroupBy(i => i.ProductType)
                .Select(g => new
                {
                    productType = g.Key,
                    count = g.Count(),
                    totalQuantity = g.Sum(i => i.TotalQuantity),
                    bookedQuantity = g.Sum(i => i.BookedQuantity),
                    availableQuantity = g.Sum(i => i.TotalQuantity - i.BookedQuantity)
                })
                .ToListAsync();

            return Ok(new
            {
                inventory = new
                {
                    totalEntries,
                    totalProducts,
                    totalBooked,
                    totalAvailable,
                    byProductType
                },
                bookings = new
                {
                    totalRevenue,
                    currency = "GBP"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory summary");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

