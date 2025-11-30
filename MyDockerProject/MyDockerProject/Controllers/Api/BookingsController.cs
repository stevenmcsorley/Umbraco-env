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
    private readonly IEmailService _emailService;
    private readonly IPaymentService _paymentService;

    public BookingsController(
        BookingService bookingService,
        InventoryService inventoryService,
        BookingDbContext context,
        IEmailService emailService,
        IPaymentService paymentService)
    {
        _bookingService = bookingService;
        _inventoryService = inventoryService;
        _context = context;
        _emailService = emailService;
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] dynamic requestData)
    {
        try
        {
            // Handle both string and Guid productId
            var productIdStr = requestData.productId?.ToString();
            if (string.IsNullOrEmpty(productIdStr) || !Guid.TryParse(productIdStr, out var productId))
            {
                return BadRequest(new { error = "Invalid productId" });
            }

            Guid? userId = null;
            if (requestData.userId != null && Guid.TryParse(requestData.userId.ToString(), out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var request = new CreateBookingRequest
            {
                ProductId = productId,
                ProductType = requestData.productType?.ToString() ?? "Room",
                CheckIn = DateTime.Parse(requestData.checkIn?.ToString() ?? DateTime.UtcNow.ToString()),
                CheckOut = requestData.checkOut != null ? DateTime.Parse(requestData.checkOut.ToString()) : null,
                Quantity = requestData.quantity != null ? (int)requestData.quantity : 1,
                GuestFirstName = requestData.guestFirstName?.ToString() ?? "",
                GuestLastName = requestData.guestLastName?.ToString() ?? "",
                GuestEmail = requestData.guestEmail?.ToString() ?? "",
                GuestPhone = requestData.guestPhone?.ToString(),
                TotalPrice = requestData.totalPrice != null ? (decimal)requestData.totalPrice : 0,
                Currency = requestData.currency?.ToString(),
                AdditionalData = requestData.additionalData?.ToString(),
                UserId = userId
            };

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

            // Process payment
            var paymentRequest = new PaymentRequest
            {
                Amount = request.TotalPrice,
                Currency = request.Currency ?? "GBP",
                Description = $"Booking for {request.ProductType} - {request.ProductId}",
                CustomerEmail = request.GuestEmail,
                CustomerName = $"{request.GuestFirstName} {request.GuestLastName}",
                Metadata = new Dictionary<string, string>
                {
                    { "productId", request.ProductId.ToString() },
                    { "productType", request.ProductType },
                    { "checkIn", request.CheckIn.ToString("O") }
                }
            };

            var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest);

            if (!paymentResult.Success)
            {
                return BadRequest(new
                {
                    error = "Payment processing failed",
                    paymentError = paymentResult.ErrorMessage
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
                request.UserId,
                paymentResult.PaymentId,
                paymentResult.TransactionId,
                "Paid",
                DateTime.UtcNow
            );

            // Send confirmation email (fire and forget)
            _ = Task.Run(async () =>
            {
                await _emailService.SendBookingConfirmationAsync(
                    booking.GuestEmail,
                    $"{booking.GuestFirstName} {booking.GuestLastName}",
                    booking.BookingReference,
                    booking.TotalPrice,
                    booking.Currency,
                    booking.CheckIn,
                    booking.CheckOut
                );
            });

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
            var booking = await _bookingService.GetBookingByReferenceAsync(bookingReference);
            if (booking == null)
            {
                return NotFound(new { error = "Booking not found" });
            }

            await _bookingService.CancelBookingAsync(bookingReference);

            // Send cancellation email (fire and forget)
            _ = Task.Run(async () =>
            {
                await _emailService.SendBookingCancellationAsync(
                    booking.GuestEmail,
                    $"{booking.GuestFirstName} {booking.GuestLastName}",
                    bookingReference
                );
            });

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
    
    // Accept productId as string (from booking engine) and convert to Guid
    public void SetProductId(string productId)
    {
        if (Guid.TryParse(productId, out var guid))
        {
            ProductId = guid;
        }
    }
}

