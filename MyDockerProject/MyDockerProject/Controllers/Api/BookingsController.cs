using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyDockerProject.Data;
using MyDockerProject.Models;
using MyDockerProject.Services;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core;

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
    private readonly Services.IUserService _userService;
    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;
    private readonly IPublishedContentQuery _publishedContentQuery;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        BookingService bookingService,
        InventoryService inventoryService,
        BookingDbContext context,
        IEmailService emailService,
        IPaymentService paymentService,
        Services.IUserService userService,
        IContentService contentService,
        IMediaService mediaService,
        IPublishedContentQuery publishedContentQuery,
        ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _inventoryService = inventoryService;
        _context = context;
        _emailService = emailService;
        _paymentService = paymentService;
        _userService = userService;
        _contentService = contentService;
        _mediaService = mediaService;
        _publishedContentQuery = publishedContentQuery;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] System.Text.Json.JsonElement requestData)
    {
        try
        {
            // Handle both string and Guid productId
            if (!requestData.TryGetProperty("productId", out var productIdElement))
            {
                return BadRequest(new { error = "Invalid productId" });
            }
            
            var productIdStr = productIdElement.GetString();
            if (string.IsNullOrEmpty(productIdStr))
            {
                return BadRequest(new { error = "Invalid productId" });
            }
            
            if (!Guid.TryParse(productIdStr, out Guid productId))
            {
                return BadRequest(new { error = "Invalid productId" });
            }

            Guid? userId = null;
            if (requestData.TryGetProperty("userId", out var userIdElement))
            {
                var userIdStr = userIdElement.GetString();
                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out Guid parsedUserId))
                {
                    userId = parsedUserId;
                    _logger.LogInformation("[BookingsController] Parsed userId: {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("[BookingsController] Failed to parse userId: {UserIdStr}", userIdStr ?? "null");
                }
            }
            else
            {
                _logger.LogInformation("[BookingsController] No userId provided in request");
            }

            // If userId is provided but guest details are missing, fetch user details
            string guestFirstName = requestData.TryGetProperty("guestFirstName", out var fnElement) ? fnElement.GetString() ?? "" : "";
            string guestLastName = requestData.TryGetProperty("guestLastName", out var lnElement) ? lnElement.GetString() ?? "" : "";
            string guestEmail = requestData.TryGetProperty("guestEmail", out var emailElement) ? emailElement.GetString() ?? "" : "";
            string? guestPhone = requestData.TryGetProperty("guestPhone", out var phoneElement) ? phoneElement.GetString() : null;

            if (userId.HasValue && (string.IsNullOrEmpty(guestFirstName) || string.IsNullOrEmpty(guestEmail)))
            {
                var user = await _userService.GetUserByIdAsync(userId.Value);
                if (user != null)
                {
                    if (string.IsNullOrEmpty(guestFirstName))
                        guestFirstName = user.FirstName;
                    if (string.IsNullOrEmpty(guestLastName))
                        guestLastName = user.LastName;
                    if (string.IsNullOrEmpty(guestEmail))
                        guestEmail = user.Email;
                    if (string.IsNullOrEmpty(guestPhone))
                        guestPhone = user.Phone;
                }
            }

            var request = new CreateBookingRequest
            {
                ProductId = productId,
                ProductType = requestData.TryGetProperty("productType", out var ptElement) ? ptElement.GetString() ?? "Room" : "Room",
                CheckIn = requestData.TryGetProperty("checkIn", out var ciElement) 
                    ? DateTime.Parse(ciElement.GetString() ?? DateTime.UtcNow.ToString("O")) 
                    : DateTime.UtcNow,
                CheckOut = requestData.TryGetProperty("checkOut", out var coElement) && coElement.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? DateTime.Parse(coElement.GetString() ?? "")
                    : null,
                Quantity = requestData.TryGetProperty("quantity", out var qElement) ? qElement.GetInt32() : 1,
                GuestFirstName = guestFirstName,
                GuestLastName = guestLastName,
                GuestEmail = guestEmail,
                GuestPhone = guestPhone,
                TotalPrice = requestData.TryGetProperty("totalPrice", out var tpElement) ? tpElement.GetDecimal() : 0,
                Currency = requestData.TryGetProperty("currency", out var currElement) ? currElement.GetString() : null,
                AdditionalData = requestData.TryGetProperty("additionalData", out var adElement) && adElement.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? adElement.GetString()
                    : null,
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

            // Fetch product (room/event) and hotel details for the response
            string? productName = null;
            string? hotelName = null;
            string? hotelLocation = null;
            string? roomImage = null;
            
            try
            {
                var productContent = _contentService.GetById(booking.ProductId);
                if (productContent != null)
                {
                    productName = productContent.GetValue<string>("roomName") 
                        ?? productContent.GetValue<string>("eventName") 
                        ?? productContent.Name;
                    
                    // Get room image (heroImage)
                    var heroImageValue = productContent.GetValue("heroImage");
                    roomImage = GetMediaUrl(heroImageValue);
                    
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
                _logger.LogError(ex, "[BookingsController] Error fetching product/hotel details");
            }
            
            // Helper method to get media URL (similar to HotelsController)
            string? GetMediaUrl(object? mediaValue)
            {
                if (mediaValue == null) return null;
                
                // Handle string that might be JSON (MediaPicker3 format)
                if (mediaValue is string jsonString)
                {
                    // Try to parse as JSON array
                    if (jsonString.TrimStart().StartsWith("["))
                    {
                        try
                        {
                            var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(jsonString);
                            if (json != null && json.Length > 0)
                            {
                                var firstItem = json[0];
                                if (firstItem.TryGetProperty("mediaKey", out var mediaKeyElement) && Guid.TryParse(mediaKeyElement.GetString(), out var mediaGuid))
                                {
                                    var publishedMediaItem = _publishedContentQuery.Media(mediaGuid);
                                    if (publishedMediaItem != null)
                                    {
                                        return publishedMediaItem.Url();
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    // Try to parse as GUID string
                    if (Guid.TryParse(jsonString, out var guid))
                    {
                        var publishedMediaItem = _publishedContentQuery.Media(guid);
                        if (publishedMediaItem != null)
                        {
                            return publishedMediaItem.Url();
                        }
                        
                        var media = _mediaService.GetById(guid);
                        if (media != null)
                        {
                            var fileValue = media.GetValue<string>("umbracoFile");
                            if (!string.IsNullOrEmpty(fileValue))
                            {
                                if (fileValue.TrimStart().StartsWith("{"))
                                {
                                    try
                                    {
                                        var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(fileValue);
                                        if (json.TryGetProperty("src", out var src))
                                        {
                                            return src.GetString();
                                        }
                                    }
                                    catch { }
                                }
                                return fileValue;
                            }
                        }
                    }
                    // Assume it's a direct URL string
                    if (Uri.IsWellFormedUriString(jsonString, UriKind.RelativeOrAbsolute))
                    {
                        return jsonString;
                    }
                }
                
                // Handle GUID directly
                if (mediaValue is Guid mediaGuidDirect)
                {
                    var publishedMediaItem = _publishedContentQuery.Media(mediaGuidDirect);
                    if (publishedMediaItem != null)
                    {
                        return publishedMediaItem.Url();
                    }
                    
                    var media = _mediaService.GetById(mediaGuidDirect);
                    if (media != null)
                    {
                        var fileValue = media.GetValue<string>("umbracoFile");
                        if (!string.IsNullOrEmpty(fileValue))
                        {
                            if (fileValue.TrimStart().StartsWith("{"))
                            {
                                try
                                {
                                    var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(fileValue);
                                    if (json.TryGetProperty("src", out var src))
                                    {
                                        return src.GetString();
                                    }
                                }
                                catch { }
                            }
                            return fileValue;
                        }
                    }
                }
                
                // Handle Udi
                if (mediaValue is Udi mediaUdi && mediaUdi is GuidUdi guidUdi2)
                {
                    var publishedMediaItem = _publishedContentQuery.Media(guidUdi2.Guid);
                    if (publishedMediaItem != null)
                    {
                        return publishedMediaItem.Url();
                    }
                    
                    var media = _mediaService.GetById(guidUdi2.Guid);
                    if (media != null)
                    {
                        var fileValue = media.GetValue<string>("umbracoFile");
                        if (!string.IsNullOrEmpty(fileValue))
                        {
                            if (fileValue.TrimStart().StartsWith("{"))
                            {
                                try
                                {
                                    var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(fileValue);
                                    if (json.TryGetProperty("src", out var src))
                                    {
                                        return src.GetString();
                                    }
                                }
                                catch { }
                            }
                            return fileValue;
                        }
                    }
                }
                
                return null;
            }
            
            // Log booking creation for debugging
            _logger.LogInformation("[BookingsController] Booking created - Id: {BookingId}, UserId: {UserId}, ProductId: {ProductId}, Reference: {Reference}", 
                booking.Id, booking.UserId?.ToString() ?? "NULL", booking.ProductId, booking.BookingReference);

            // Parse AdditionalData to extract events and add-ons
            List<object>? events = null;
            List<object>? addOns = null;
            
            if (!string.IsNullOrEmpty(booking.AdditionalData))
            {
                try
                {
                    var additionalDataJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(booking.AdditionalData);
                    if (additionalDataJson.TryGetProperty("events", out var eventsElement) && eventsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        events = new List<object>();
                        foreach (var eventItem in eventsElement.EnumerateArray())
                        {
                            events.Add(System.Text.Json.JsonSerializer.Deserialize<object>(eventItem.GetRawText()) ?? new object());
                        }
                    }
                    if (additionalDataJson.TryGetProperty("addOns", out var addOnsElement) && addOnsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        addOns = new List<object>();
                        foreach (var addOnItem in addOnsElement.EnumerateArray())
                        {
                            addOns.Add(System.Text.Json.JsonSerializer.Deserialize<object>(addOnItem.GetRawText()) ?? new object());
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[BookingsController] Failed to parse AdditionalData: {AdditionalData}", booking.AdditionalData);
                }
            }
            
            // Log room image for debugging
            _logger.LogInformation("[BookingsController] Room image: {RoomImage}", roomImage ?? "NULL");
            
            return Ok(new
            {
                bookingId = booking.Id,
                bookingReference = booking.BookingReference,
                productId = booking.ProductId,
                productType = booking.ProductType,
                productName = productName,
                hotelName = hotelName,
                hotelLocation = hotelLocation,
                roomImage = roomImage,
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
                userId = booking.UserId,
                events = events,
                addOns = addOns
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

