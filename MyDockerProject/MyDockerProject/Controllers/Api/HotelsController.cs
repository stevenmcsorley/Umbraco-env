using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;
using MyDockerProject.Helpers;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class HotelsController : ControllerBase
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaService _mediaService;
    private readonly IPublishedContentQuery _publishedContentQuery;

    public HotelsController(
        IContentService contentService, 
        IContentTypeService contentTypeService, 
        IMediaService mediaService,
        IPublishedContentQuery publishedContentQuery)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _mediaService = mediaService;
        _publishedContentQuery = publishedContentQuery;
    }

    private string? GetMediaUrl(object? mediaValue)
    {
        if (mediaValue == null) return null;
        
        // Try to get as IPublishedContent first (for published media)
        if (mediaValue is IPublishedContent publishedContent)
        {
            return publishedContent.Url();
        }
        
        // Handle JSON array format from MediaPicker3 (most common)
        // Format: "[{\"key\":\"...\",\"mediaKey\":\"guid\",\"mediaTypeAlias\":\"Image\",...}]"
        if (mediaValue is string jsonString)
        {
            // If it's already a URL, return it
            if (jsonString.StartsWith("http://") || jsonString.StartsWith("https://") || jsonString.StartsWith("/"))
            {
                return jsonString;
            }
            
            // Try to parse as JSON array
            if (jsonString.TrimStart().StartsWith("["))
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(jsonString);
                    if (json.ValueKind == System.Text.Json.JsonValueKind.Array && json.GetArrayLength() > 0)
                    {
                        var firstItem = json[0];
                        if (firstItem.TryGetProperty("mediaKey", out var mediaKeyElement))
                        {
                            var mediaKeyString = mediaKeyElement.GetString();
                            if (!string.IsNullOrEmpty(mediaKeyString) && Guid.TryParse(mediaKeyString, out var parsedMediaGuid))
                            {
                                // Try to get as published content first
                                var publishedMediaItem = _publishedContentQuery.Media(parsedMediaGuid);
                                if (publishedMediaItem != null)
                                {
                                    return publishedMediaItem.Url();
                                }
                                
                                // Fallback to IMediaService
                                var media = _mediaService.GetById(parsedMediaGuid);
                                if (media != null)
                                {
                                    var fileValue = media.GetValue<string>("umbracoFile");
                                    if (!string.IsNullOrEmpty(fileValue))
                                    {
                                        // Handle JSON string format from umbracoFile
                                        if (fileValue.TrimStart().StartsWith("{"))
                                        {
                                            try
                                            {
                                                var fileJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(fileValue);
                                                if (fileJson.TryGetProperty("src", out var src))
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
                        }
                    }
                }
                catch { }
            }
            
            // Try to parse as Udi
            if (UdiParser.TryParse(jsonString, out var udi) && udi is GuidUdi guidUdi)
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
            
            // Try to parse as GUID
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
        }
        
        // Handle GUID directly
        if (mediaValue is Guid mediaGuid)
        {
            var publishedMediaItem = _publishedContentQuery.Media(mediaGuid);
            if (publishedMediaItem != null)
            {
                return publishedMediaItem.Url();
            }
            
            var media = _mediaService.GetById(mediaGuid);
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

    private IEnumerable<string>? GetMediaUrls(object? mediaValue)
    {
        if (mediaValue == null) return null;
        
        var urls = new List<string>();
        
        // Handle string that might be JSON array (MediaPicker3 format)
        if (mediaValue is string jsonString)
        {
            // Try to parse as JSON array
            if (jsonString.TrimStart().StartsWith("["))
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(jsonString);
                    if (json.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in json.EnumerateArray())
                        {
                            // Handle MediaPicker3 format: {"key":"...","mediaKey":"guid",...}
                            if (item.TryGetProperty("mediaKey", out var mediaKeyElement))
                            {
                                var mediaKeyString = mediaKeyElement.GetString();
                                if (!string.IsNullOrEmpty(mediaKeyString))
                                {
                                    var url = GetMediaUrl(mediaKeyString);
                                    if (!string.IsNullOrEmpty(url))
                                    {
                                        urls.Add(url);
                                    }
                                }
                            }
                            // Fallback: try to get as string
                            else if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                var url = GetMediaUrl(item.GetString());
                                if (!string.IsNullOrEmpty(url))
                                {
                                    urls.Add(url);
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            else
            {
                // Single value - try to get URL
                var url = GetMediaUrl(jsonString);
                if (!string.IsNullOrEmpty(url))
                {
                    urls.Add(url);
                }
            }
        }
        // Handle IEnumerable
        else if (mediaValue is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var url = GetMediaUrl(item);
                if (!string.IsNullOrEmpty(url))
                {
                    urls.Add(url);
                }
            }
        }
        else
        {
            // Single item
            var url = GetMediaUrl(mediaValue);
            if (!string.IsNullOrEmpty(url))
            {
                urls.Add(url);
            }
        }
        
        return urls.Any() ? urls : null;
    }

    [HttpGet]
    public IActionResult GetHotels()
    {
        var contentType = _contentTypeService.Get("hotel");
        if (contentType == null)
        {
            return Ok(new List<object>()); // Return empty list if content type doesn't exist
        }

        var hotels = _contentService.GetPagedOfType(contentType.Id, 0, int.MaxValue, out var total, null);
        
        var result = hotels.Select(h =>
        {
            var hotelName = h.GetValue<string>("hotelName") ?? h.Name;
            var slug = UrlHelper.ToSlug(hotelName);
            
            // Get MediaPicker3 values directly from Properties collection
            object? heroImageValue = null;
            object? galleryImagesValue = null;
            
            var heroImageProp = h.Properties.FirstOrDefault(p => p.Alias == "heroImage");
            if (heroImageProp != null)
            {
                heroImageValue = heroImageProp.GetValue();
            }
            
            var galleryImagesProp = h.Properties.FirstOrDefault(p => p.Alias == "galleryImages");
            if (galleryImagesProp != null)
            {
                galleryImagesValue = galleryImagesProp.GetValue();
            }
            
            return new
            {
                id = h.Key,
                name = hotelName,
                slug = slug,
                description = h.GetValue<string>("description"),
                shortDescription = h.GetValue<string>("shortDescription"),
                heroImage = GetMediaUrl(heroImageValue),
                galleryImages = GetMediaUrls(galleryImagesValue),
                address = h.GetValue<string>("address"),
                city = h.GetValue<string>("city"),
                country = h.GetValue<string>("country"),
                phone = h.GetValue<string>("phone"),
                email = h.GetValue<string>("email"),
                amenities = h.GetValue<string>("amenities"),
                highlights = h.GetValue<string>("highlights"),
                features = h.GetValue<string>("features"),
                url = $"/hotels/{slug}"
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{identifier}")]
    public IActionResult GetHotel(string identifier)
    {
        IContent? hotel = null;
        
        // Try to find by GUID first
        if (Guid.TryParse(identifier, out var guid))
        {
            hotel = _contentService.GetById(guid);
        }
        
        // If not found by GUID, try to find by slug
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            var contentType = _contentTypeService.Get("hotel");
            if (contentType != null)
            {
                var hotels = _contentService.GetPagedOfType(contentType.Id, 0, int.MaxValue, out _, null);
                hotel = hotels.FirstOrDefault(h =>
                {
                    var hotelName = h.GetValue<string>("hotelName") ?? h.Name;
                    var slug = UrlHelper.ToSlug(hotelName);
                    return slug.Equals(identifier, StringComparison.OrdinalIgnoreCase);
                });
            }
        }
        
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            return NotFound();
        }

        var hotelName = hotel.GetValue<string>("hotelName") ?? hotel.Name;
        var slug = UrlHelper.ToSlug(hotelName);
        
        // Get MediaPicker3 values directly from Properties collection
        object? heroImageValue = null;
        object? galleryImagesValue = null;
        
        var heroImageProp = hotel.Properties.FirstOrDefault(p => p.Alias == "heroImage");
        if (heroImageProp != null)
        {
            heroImageValue = heroImageProp.GetValue();
        }
        
        var galleryImagesProp = hotel.Properties.FirstOrDefault(p => p.Alias == "galleryImages");
        if (galleryImagesProp != null)
        {
            galleryImagesValue = galleryImagesProp.GetValue();
        }
        
        var result = new
        {
            id = hotel.Key,
            name = hotelName,
            slug = slug,
            description = hotel.GetValue<string>("description"),
            shortDescription = hotel.GetValue<string>("shortDescription"),
            heroImage = GetMediaUrl(heroImageValue),
            galleryImages = GetMediaUrls(galleryImagesValue),
            address = hotel.GetValue<string>("address"),
            city = hotel.GetValue<string>("city"),
            country = hotel.GetValue<string>("country"),
            phone = hotel.GetValue<string>("phone"),
            email = hotel.GetValue<string>("email"),
            amenities = hotel.GetValue<string>("amenities"),
            highlights = hotel.GetValue<string>("highlights"),
            features = hotel.GetValue<string>("features"),
            url = $"/hotels/{slug}"
        };

        return Ok(result);
    }

    [HttpGet("{identifier}/rooms")]
    public IActionResult GetRooms(string identifier)
    {
        IContent? hotel = null;
        
        // Try to find by GUID first
        if (Guid.TryParse(identifier, out var guid))
        {
            hotel = _contentService.GetById(guid);
        }
        
        // If not found by GUID, try to find by slug
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            var contentType = _contentTypeService.Get("hotel");
            if (contentType != null)
            {
                var hotels = _contentService.GetPagedOfType(contentType.Id, 0, int.MaxValue, out _, null);
                hotel = hotels.FirstOrDefault(h =>
                {
                    var hotelName = h.GetValue<string>("hotelName") ?? h.Name;
                    var slug = UrlHelper.ToSlug(hotelName);
                    return slug.Equals(identifier, StringComparison.OrdinalIgnoreCase);
                });
            }
        }
        
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            return NotFound();
        }

        var hotelName = hotel.GetValue<string>("hotelName") ?? hotel.Name;
        var hotelSlug = UrlHelper.ToSlug(hotelName);

        var rooms = _contentService.GetPagedChildren(hotel.Id, 0, int.MaxValue, out _)
            .Where(c => c.ContentType.Alias == "room");

        var result = rooms.Select(r =>
        {
            var roomName = r.GetValue<string>("roomName") ?? r.Name;
            var roomSlug = UrlHelper.ToSlug(roomName);
            var heroImageValue = r.GetValue("heroImage");
            var roomImagesValue = r.GetValue("roomImages");
            
            return new
            {
                id = r.Key,
                name = roomName,
                slug = roomSlug,
                description = r.GetValue<string>("description"),
                maxOccupancy = r.GetValue<int?>("maxOccupancy"),
                priceFrom = r.GetValue<decimal?>("priceFrom"),
                roomType = r.GetValue<string>("roomType"),
                heroImage = GetMediaUrl(heroImageValue),
                roomImages = GetMediaUrls(roomImagesValue),
                size = r.GetValue<string>("size"),
                bedType = r.GetValue<string>("bedType"),
                furnishings = r.GetValue<string>("furnishings"),
                specifications = r.GetValue<string>("specifications"),
                features = r.GetValue<string>("features"),
                hotelServices = r.GetValue<string>("hotelServices"),
                url = $"/hotels/{hotelSlug}/rooms/{roomSlug}"
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{identifier}/offers")]
    public IActionResult GetOffers(string identifier)
    {
        IContent? hotel = null;
        
        // Try to find by GUID first
        if (Guid.TryParse(identifier, out var guid))
        {
            hotel = _contentService.GetById(guid);
        }
        
        // If not found by GUID, try to find by slug
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            var contentType = _contentTypeService.Get("hotel");
            if (contentType != null)
            {
                var hotels = _contentService.GetPagedOfType(contentType.Id, 0, int.MaxValue, out _, null);
                hotel = hotels.FirstOrDefault(h =>
                {
                    var hotelName = h.GetValue<string>("hotelName") ?? h.Name;
                    var slug = UrlHelper.ToSlug(hotelName);
                    return slug.Equals(identifier, StringComparison.OrdinalIgnoreCase);
                });
            }
        }
        
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            return NotFound();
        }

        var offers = _contentService.GetPagedChildren(hotel.Id, 0, int.MaxValue, out _)
            .Where(c => c.ContentType.Alias == "offer");

        var hotelName = hotel.GetValue<string>("hotelName") ?? hotel.Name;
        var hotelSlug = UrlHelper.ToSlug(hotelName);

        var result = offers.Select(o =>
        {
            object? offerImageValue = null;
            var offerImageProp = o.Properties.FirstOrDefault(p => p.Alias == "image");
            if (offerImageProp != null)
            {
                offerImageValue = offerImageProp.GetValue();
            }
            
            var offerName = o.GetValue<string>("offerName") ?? o.Name;
            var offerSlug = UrlHelper.ToSlug(offerName);
            
            return new
            {
                id = o.Key,
                name = offerName,
                slug = offerSlug,
                description = o.GetValue<string>("description"),
                discount = o.GetValue<decimal?>("discount"),
                validFrom = o.GetValue<DateTime?>("validFrom"),
                validTo = o.GetValue<DateTime?>("validTo"),
                minNights = o.GetValue<int?>("minNights") ?? 0,
                minAdvanceBookingDays = o.GetValue<int?>("minAdvanceBookingDays") ?? 0,
                image = GetMediaUrl(offerImageValue),
                url = $"/hotels/{hotelSlug}/offers/{offerSlug}"
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{identifier}/events")]
    public IActionResult GetEvents(string identifier)
    {
        IContent? hotel = null;
        
        // Try to find by GUID first
        if (Guid.TryParse(identifier, out var guid))
        {
            hotel = _contentService.GetById(guid);
        }
        
        // If not found by GUID, try to find by slug
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            var contentType = _contentTypeService.Get("hotel");
            if (contentType != null)
            {
                var hotels = _contentService.GetPagedOfType(contentType.Id, 0, int.MaxValue, out _, null);
                hotel = hotels.FirstOrDefault(h =>
                {
                    var hotelName = h.GetValue<string>("hotelName") ?? h.Name;
                    var slug = UrlHelper.ToSlug(hotelName);
                    return slug.Equals(identifier, StringComparison.OrdinalIgnoreCase);
                });
            }
        }
        
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            return NotFound();
        }

        var hotelName = hotel.GetValue<string>("hotelName") ?? hotel.Name;
        var hotelSlug = UrlHelper.ToSlug(hotelName);
        
        var events = _contentService.GetPagedChildren(hotel.Id, 0, int.MaxValue, out _)
            .Where(c => c.ContentType.Alias == "event");

        var result = events.Select(e =>
        {
            object? eventImageValue = null;
            var eventImageProp = e.Properties.FirstOrDefault(p => p.Alias == "heroImage" || p.Alias == "eventImage");
            if (eventImageProp != null)
            {
                eventImageValue = eventImageProp.GetValue();
            }
            
            return new
            {
                id = e.Key,
                name = e.GetValue<string>("eventName") ?? e.Name,
                slug = UrlHelper.ToSlug(e.GetValue<string>("eventName") ?? e.Name),
                description = e.GetValue<string>("description"),
                eventDate = e.GetValue<DateTime?>("eventDate"),
                price = e.GetValue<decimal?>("price"),
                location = e.GetValue<string>("location"),
                image = GetMediaUrl(eventImageValue),
                url = $"/hotels/{hotelSlug}/events/{UrlHelper.ToSlug(e.GetValue<string>("eventName") ?? e.Name)}"
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{identifier}/availability")]
    public IActionResult GetAvailability(string identifier, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        IContent? hotel = null;
        
        // Try to find by GUID first
        if (Guid.TryParse(identifier, out var guid))
        {
            hotel = _contentService.GetById(guid);
        }
        
        // If not found by GUID, try to find by slug
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            var contentType = _contentTypeService.Get("hotel");
            if (contentType != null)
            {
                var hotels = _contentService.GetPagedOfType(contentType.Id, 0, int.MaxValue, out _, null);
                hotel = hotels.FirstOrDefault(h =>
                {
                    var hotelName = h.GetValue<string>("hotelName") ?? h.Name;
                    var slug = UrlHelper.ToSlug(hotelName);
                    return slug.Equals(identifier, StringComparison.OrdinalIgnoreCase);
                });
            }
        }
        
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            return NotFound();
        }

        var rooms = _contentService.GetPagedChildren(hotel.Id, 0, int.MaxValue, out var total)
            .Where(c => c.ContentType.Alias == "room");

        var result = new
        {
            hotelId = hotel.Key,
            from = from ?? DateTime.Today,
            to = to ?? DateTime.Today.AddDays(30),
            rooms = rooms.Select(r => new
            {
                roomId = r.Key,
                roomName = r.GetValue<string>("roomName") ?? r.Name,
                available = true // Placeholder - actual availability comes from booking engine
            }).ToList()
        };

        return Ok(result);
    }

    [HttpGet("{identifier}/debug-properties")]
    public IActionResult GetHotelPropertiesDebug(string identifier)
    {
        IContent? hotel = null;
        
        // Try to find by GUID first
        if (Guid.TryParse(identifier, out var guid))
        {
            hotel = _contentService.GetById(guid);
        }
        
        // If not found by GUID, try to find by slug
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            var contentType = _contentTypeService.Get("hotel");
            if (contentType != null)
            {
                var hotels = _contentService.GetPagedOfType(contentType.Id, 0, int.MaxValue, out _, null);
                hotel = hotels.FirstOrDefault(h =>
                {
                    var hotelName = h.GetValue<string>("hotelName") ?? h.Name;
                    var slug = UrlHelper.ToSlug(hotelName);
                    return slug.Equals(identifier, StringComparison.OrdinalIgnoreCase);
                });
            }
        }
        
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            return NotFound();
        }

        var heroImageProp = hotel.Properties.FirstOrDefault(p => p.Alias == "heroImage");
        var galleryImagesProp = hotel.Properties.FirstOrDefault(p => p.Alias == "galleryImages");
        
        var heroImageValue = heroImageProp?.GetValue();
        var galleryImagesValue = galleryImagesProp?.GetValue();
        
        return Ok(new
        {
            hotelName = hotel.Name,
            hotelPublished = hotel.Published,
            heroImageProperty = new
            {
                exists = heroImageProp != null,
                hasValue = heroImageValue != null,
                value = heroImageValue?.ToString(),
                valueType = heroImageValue?.GetType().Name,
                valueTypeFull = heroImageValue?.GetType().FullName,
                isUdi = heroImageValue is Udi,
                isString = heroImageValue is string,
                isGuid = heroImageValue is Guid,
                resolvedUrl = GetMediaUrl(heroImageValue)
            },
            galleryImagesProperty = new
            {
                exists = galleryImagesProp != null,
                hasValue = galleryImagesValue != null,
                value = galleryImagesValue?.ToString(),
                valueType = galleryImagesValue?.GetType().Name,
                valueTypeFull = galleryImagesValue?.GetType().FullName,
                isEnumerable = galleryImagesValue is System.Collections.IEnumerable,
                resolvedUrls = GetMediaUrls(galleryImagesValue)
            }
        });
    }
}

