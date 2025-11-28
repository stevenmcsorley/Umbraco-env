using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class HotelsController : ControllerBase
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;

    public HotelsController(IContentService contentService, IContentTypeService contentTypeService)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
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
        
        var result = hotels.Select(h => new
        {
            id = h.Key,
            name = h.GetValue<string>("hotelName") ?? h.Name,
            description = h.GetValue<string>("description"),
            address = h.GetValue<string>("address"),
            city = h.GetValue<string>("city"),
            country = h.GetValue<string>("country"),
            phone = h.GetValue<string>("phone"),
            email = h.GetValue<string>("email"),
            url = $"/{h.Name.ToLower().Replace(" ", "-")}"
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetHotel(Guid id)
    {
        var hotel = _contentService.GetById(id);
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            return NotFound();
        }

        var result = new
        {
            id = hotel.Key,
            name = hotel.GetValue<string>("hotelName") ?? hotel.Name,
            description = hotel.GetValue<string>("description"),
            address = hotel.GetValue<string>("address"),
            city = hotel.GetValue<string>("city"),
            country = hotel.GetValue<string>("country"),
            phone = hotel.GetValue<string>("phone"),
            email = hotel.GetValue<string>("email"),
            url = $"/{hotel.Name.ToLower().Replace(" ", "-")}"
        };

        return Ok(result);
    }

    [HttpGet("{id:guid}/rooms")]
    public IActionResult GetRooms(Guid id)
    {
        var hotel = _contentService.GetById(id);
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            return NotFound();
        }

        var rooms = _contentService.GetPagedChildren(hotel.Id, 0, int.MaxValue, out var total)
            .Where(c => c.ContentType.Alias == "room");

        var result = rooms.Select(r => new
        {
            id = r.Key,
            name = r.GetValue<string>("roomName") ?? r.Name,
            description = r.GetValue<string>("description"),
            maxOccupancy = r.GetValue<int?>("maxOccupancy"),
            priceFrom = r.GetValue<decimal?>("priceFrom"),
            roomType = r.GetValue<string>("roomType"),
            url = $"/{r.Name.ToLower().Replace(" ", "-")}"
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}/offers")]
    public IActionResult GetOffers(Guid id)
    {
        var hotel = _contentService.GetById(id);
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            return NotFound();
        }

        var hotelContent = _contentService.GetById(id);
        if (hotelContent == null)
        {
            return NotFound();
        }
        var offers = _contentService.GetPagedChildren(hotelContent.Id, 0, int.MaxValue, out var total)
            .Where(c => c.ContentType.Alias == "offer");

        var result = offers.Select(o => new
        {
            id = o.Key,
            name = o.GetValue<string>("offerName") ?? o.Name,
            description = o.GetValue<string>("description"),
            discount = o.GetValue<decimal?>("discount"),
            validFrom = o.GetValue<DateTime?>("validFrom"),
            validTo = o.GetValue<DateTime?>("validTo"),
            url = $"/{o.Name.ToLower().Replace(" ", "-")}"
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}/availability")]
    public IActionResult GetAvailability(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var hotel = _contentService.GetById(id);
        if (hotel == null || hotel.ContentType.Alias != "hotel")
        {
            return NotFound();
        }

        // This endpoint returns basic availability structure
        // Actual availability logic will be handled by the Booking Engine backend
        var hotelContent = _contentService.GetById(id);
        if (hotelContent == null)
        {
            return NotFound();
        }
        var rooms = _contentService.GetPagedChildren(hotelContent.Id, 0, int.MaxValue, out var total)
            .Where(c => c.ContentType.Alias == "room");

        var result = new
        {
            hotelId = id,
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
}

