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

    public IActionResult Room(string hotelId, string roomId, string layout = "Main")
    {
        ViewBag.HotelId = hotelId;
        ViewBag.RoomId = roomId;
        ViewData["Layout"] = $"Layouts/{layout}.cshtml";
        return View("room");
    }
}

