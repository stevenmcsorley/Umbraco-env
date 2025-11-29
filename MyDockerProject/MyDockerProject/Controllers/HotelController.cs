using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services;
using MyDockerProject.Helpers;

namespace MyDockerProject.Controllers;

public class HotelController : Controller
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly HttpClient _httpClient;

    public HotelController(
        IContentService contentService,
        IContentTypeService contentTypeService)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:44372") };
    }

    public IActionResult HotelList(string layout = "Main")
    {
        ViewData["Layout"] = $"Layouts/{layout}.cshtml";
        return View("hotelList");
    }

    public IActionResult HotelDetails(string id, string layout = "Main")
    {
        // id can be either a GUID or a slug
        ViewBag.HotelId = id;
        ViewBag.HotelSlug = id; // For consistency with other views
        ViewData["Layout"] = $"Layouts/{layout}.cshtml";
        return View("hotel");
    }

    public IActionResult HotelRooms(string hotelId, string layout = "Main")
    {
        ViewBag.HotelId = hotelId;
        ViewData["Layout"] = $"Layouts/{layout}.cshtml";
        return View("hotelRooms");
    }

    public IActionResult Room(string hotelSlug, string roomSlug, string layout = "Main")
    {
        // hotelSlug and roomSlug can be either GUIDs or slugs
        ViewBag.HotelId = hotelSlug;
        ViewBag.RoomId = roomSlug;
        ViewBag.HotelSlug = hotelSlug;
        ViewBag.RoomSlug = roomSlug;
        ViewData["Layout"] = $"Layouts/{layout}.cshtml";
        return View("room");
    }

    [HttpGet]
    public IActionResult Event(string hotelSlug, string eventSlug, string layout = "Main")
    {
        // Log for debugging
        System.Diagnostics.Debug.WriteLine($"Event route matched - hotelSlug: {hotelSlug}, eventSlug: {eventSlug}");
        
        ViewBag.HotelId = hotelSlug;
        ViewBag.EventId = eventSlug;
        ViewBag.HotelSlug = hotelSlug;
        ViewBag.EventSlug = eventSlug;
        ViewData["Layout"] = $"Layouts/{layout}.cshtml";
        return View("event");
    }

    [HttpGet]
    public IActionResult Offer(string hotelSlug, string offerSlug, string layout = "Main")
    {
        // Log for debugging
        System.Diagnostics.Debug.WriteLine($"Offer route matched - hotelSlug: {hotelSlug}, offerSlug: {offerSlug}");
        
        ViewBag.HotelId = hotelSlug;
        ViewBag.OfferId = offerSlug;
        ViewBag.HotelSlug = hotelSlug;
        ViewBag.OfferSlug = offerSlug;
        ViewData["Layout"] = $"Layouts/{layout}.cshtml";
        return View("offer");
    }
}

