using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace MyDockerProject.Controllers;

public class BookingController : Controller
{
    public IActionResult Book(string layout = "Main")
    {
        ViewData["Layout"] = $"Layouts/{layout}.cshtml";
        ViewData["Title"] = "Book Your Stay";
        
        // Get user info from session if logged in
        var userId = HttpContext.Session.GetString("UserId");
        var userEmail = HttpContext.Session.GetString("UserEmail");
        var userFirstName = HttpContext.Session.GetString("UserFirstName");
        var userLastName = HttpContext.Session.GetString("UserLastName");
        var userPhone = HttpContext.Session.GetString("UserPhone");
        
        ViewBag.User = !string.IsNullOrEmpty(userId) ? new
        {
            userId = userId,
            email = userEmail ?? "",
            firstName = userFirstName ?? "",
            lastName = userLastName ?? "",
            phone = userPhone
        } : null;
        
        return View("book");
    }
}

