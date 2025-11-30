using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDockerProject.Data;
using MyDockerProject.Models;
using MyDockerProject.Services;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;
    private readonly InventoryService _inventoryService;
    private readonly BookingDbContext _context;

    public BookingsController(
        BookingService bookingService,
        InventoryService inventoryService,
        BookingDbContext context)
    {
        _bookingService = bookingService;
        _inventoryService = inventoryService;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        try
        {
            // Validate availability before creating booking
            var isAvailable = request.ProductType == "Room" && request.CheckOut.HasValue
                ? await _inventoryService.IsAvailableForRangeAsync(
                    request.ProductId, request.CheckIn, request.CheckOut.Value, request.Quantity)
                : await _inventoryService.IsAvailableAsync(
                    request.ProductId, request.CheckIn, request.Quantity);

            if (!isAvailable)
            {
                return BadRequest(new
                {
                    error = "Not enough availability for the selected dates",
                    productId = request.ProductId,
                    checkIn = request.CheckIn,
                    checkOut = request.CheckOut,
                    quantity = request.Quantity
                });
            }

            var booking = await _bookingService.CreateBookingAsync(
                request.ProductId,
                request.ProductType,
                request.CheckIn,
                request.CheckOut,
                request.Quantity,
                request.GuestFirstName,
                request.GuestLastName,
                request.GuestEmail,
                request.GuestPhone,
                request.TotalPrice,
                request.Currency ?? "GBP",
                request.AdditionalData,
                request.UserId
            );

            return Ok(new
            {
                bookingId = booking.Id,
                bookingReference = booking.BookingReference,
                productId = booking.ProductId,
                productType = booking.ProductType,
                checkIn = booking.CheckIn,
                checkOut = booking.CheckOut,
                quantity = booking.Quantity,
                totalPrice = booking.TotalPrice,
                currency = booking.Currency,
                status = booking.Status,
                createdAt = booking.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{bookingReference}")]
    public async Task<IActionResult> GetBooking(string bookingReference)
    {
        var booking = await _bookingService.GetBookingByReferenceAsync(bookingReference);
        
        if (booking == null)
        {
            return NotFound(new { error = "Booking not found" });
        }

        return Ok(new
        {
            bookingId = booking.Id,
            bookingReference = booking.BookingReference,
            productId = booking.ProductId,
            productType = booking.ProductType,
            checkIn = booking.CheckIn,
            checkOut = booking.CheckOut,
            quantity = booking.Quantity,
            guestFirstName = booking.GuestFirstName,
            guestLastName = booking.GuestLastName,
            guestEmail = booking.GuestEmail,
            guestPhone = booking.GuestPhone,
            totalPrice = booking.TotalPrice,
            currency = booking.Currency,
            status = booking.Status,
            createdAt = booking.CreatedAt,
            additionalData = booking.AdditionalData
        });
    }

    [HttpPost("{bookingReference}/cancel")]
    public async Task<IActionResult> CancelBooking(string bookingReference)
    {
        try
        {
            await _bookingService.CancelBookingAsync(bookingReference);
            return Ok(new { message = "Booking cancelled successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserBookings(Guid userId)
    {
        var bookings = await _bookingService.GetBookingsByUserAsync(userId);
        
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
}

public class CreateBookingRequest
{
    public Guid ProductId { get; set; }
    public string ProductType { get; set; } = string.Empty; // "Room" or "Event"
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int Quantity { get; set; } = 1;
    public string GuestFirstName { get; set; } = string.Empty;
    public string GuestLastName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string? GuestPhone { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Currency { get; set; }
    public string? AdditionalData { get; set; } // JSON string for add-ons, events, offers
    public Guid? UserId { get; set; }
}

