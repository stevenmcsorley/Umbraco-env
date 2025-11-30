using Microsoft.AspNetCore.Mvc;
using MyDockerProject.Services;
using Umbraco.Cms.Core.Services;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace MyDockerProject.Controllers;

public class AccountController : Controller
{
    private readonly Services.IUserService _userService;
    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;
    private readonly IPublishedContentQuery _publishedContentQuery;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        Services.IUserService userService, 
        IContentService contentService,
        IMediaService mediaService,
        IPublishedContentQuery publishedContentQuery,
        ILogger<AccountController> logger)
    {
        _userService = userService;
        _contentService = contentService;
        _mediaService = mediaService;
        _publishedContentQuery = publishedContentQuery;
        _logger = logger;
    }
    
    private string? GetMediaUrl(object? mediaValue)
    {
        if (mediaValue == null) return null;
        
        // Handle MediaWithCrops (MediaPicker3) - use Content property
        if (mediaValue is MediaWithCrops mediaWithCrops)
        {
            var media = mediaWithCrops.Content;
            if (media != null)
            {
                return media.Url();
            }
        }
        
        // Handle IEnumerable<MediaWithCrops>
        if (mediaValue is IEnumerable<MediaWithCrops> mediaList)
        {
            var firstMedia = mediaList.FirstOrDefault();
            if (firstMedia?.Content != null)
            {
                return firstMedia.Content.Url();
            }
        }
        
        // Handle Udi
        if (mediaValue is Udi mediaUdi && mediaUdi is GuidUdi guidUdi)
        {
            var publishedMediaItem = _publishedContentQuery.Media(guidUdi.Guid);
            if (publishedMediaItem != null)
            {
                return publishedMediaItem.Url();
            }
            
            var media = _mediaService.GetById(guidUdi.Guid);
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
        
        // Handle string (URL or JSON)
        if (mediaValue is string stringValue)
        {
            if (stringValue.TrimStart().StartsWith("["))
            {
                // JSON array - try to parse
                try
                {
                    var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(stringValue);
                    if (json != null && json.Length > 0)
                    {
                        if (json[0].TryGetProperty("mediaKey", out var mediaKey))
                        {
                            var guid = Guid.Parse(mediaKey.GetString()!);
                            var publishedMedia = _publishedContentQuery.Media(guid);
                            return publishedMedia?.Url();
                        }
                    }
                }
                catch { }
            }
            else if (!string.IsNullOrEmpty(stringValue))
            {
                return stringValue;
            }
        }
        
        return null;
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
        var bookingsWithDetails = new List<Models.BookingWithDetails>();
        
        foreach (var booking in bookings)
        {
            string? productName = null;
            string? hotelName = null;
            string? hotelLocation = null;
            string? roomImage = null;
            string? hotelImage = null;
            List<string>? roomFeatures = null;
            
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
                    
                    // Get room features
                    var featuresValue = productContent.GetValue("features");
                    if (featuresValue != null)
                    {
                        if (featuresValue is string featuresString && !string.IsNullOrEmpty(featuresString))
                        {
                            // Try to parse as JSON array first
                            if (featuresString.TrimStart().StartsWith("["))
                            {
                                try
                                {
                                    var jsonArray = System.Text.Json.JsonSerializer.Deserialize<string[]>(featuresString);
                                    if (jsonArray != null && jsonArray.Length > 0)
                                    {
                                        roomFeatures = jsonArray
                                            .Select(f => f?.Trim())
                                            .Where(f => !string.IsNullOrEmpty(f))
                                            .ToList();
                                    }
                                }
                                catch { }
                            }
                            
                            // If not parsed as JSON, try comma-separated, semicolon-separated, or newline-separated
                            if (roomFeatures == null || roomFeatures.Count == 0)
                            {
                                var separators = new[] { ',', ';', '\n', '\r' };
                                var splitFeatures = featuresString
                                    .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(f => f.Trim())
                                    .Where(f => !string.IsNullOrEmpty(f))
                                    .ToList();
                                
                                if (splitFeatures.Count > 0)
                                {
                                    roomFeatures = splitFeatures;
                                }
                            }
                        }
                        else if (featuresValue is System.Collections.IEnumerable featuresEnumerable && !(featuresValue is string))
                        {
                            roomFeatures = featuresEnumerable.Cast<object>()
                                .Select(f => f?.ToString()?.Trim() ?? "")
                                .Where(f => !string.IsNullOrEmpty(f))
                                .ToList();
                        }
                    }
                    
                    // Get hotel (parent of room/event)
                    if (productContent.ParentId > 0)
                    {
                        var hotelContent = _contentService.GetById(productContent.ParentId);
                        if (hotelContent != null)
                        {
                            hotelName = hotelContent.GetValue<string>("hotelName") ?? hotelContent.Name;
                            hotelLocation = hotelContent.GetValue<string>("location");
                            
                            // Get hotel image
                            var hotelImageValue = hotelContent.GetValue("heroImage");
                            hotelImage = GetMediaUrl(hotelImageValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue
                _logger.LogError(ex, "[AccountController] Error fetching details for booking {BookingId}", booking.Id);
            }
            
            bookingsWithDetails.Add(new Models.BookingWithDetails
            {
                Booking = booking,
                ProductName = productName,
                HotelName = hotelName,
                HotelLocation = hotelLocation,
                RoomImage = roomImage,
                HotelImage = hotelImage,
                RoomFeatures = roomFeatures
            });
        }
        
        // Debug logging
        _logger.LogInformation("[AccountController] UserId: {UserId}, Bookings count: {BookingsCount}", userId, bookings?.Count ?? 0);
        if (bookings != null && bookings.Count > 0)
        {
            foreach (var booking in bookings)
            {
                _logger.LogInformation("[AccountController] Booking - Id: {BookingId}, UserId: {UserId}, Reference: {Reference}", 
                    booking.Id, booking.UserId?.ToString() ?? "NULL", booking.BookingReference);
            }
        }

        ViewBag.User = user;
        ViewBag.Bookings = bookingsWithDetails;
        return View();
    }
}

