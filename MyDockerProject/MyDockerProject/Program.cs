using Microsoft.EntityFrameworkCore;
using MyDockerProject.Data;
using MyDockerProject.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add controllers for custom routes
builder.Services.AddControllers();

// Add HttpClient for booking engine proxy
builder.Services.AddHttpClient();

// Add Entity Framework for bookings and inventory
var connectionString = builder.Configuration.GetConnectionString("umbracoDbDSN");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<BookingDbContext>(options =>
        options.UseSqlServer(connectionString));
    
    // Register services
    builder.Services.AddScoped<BookingService>();
    builder.Services.AddScoped<InventoryService>();
    builder.Services.AddScoped<DataImportService>();
}

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        
        // Register booking engine proxy route FIRST (before API controllers and Umbraco routing)
        u.EndpointRouteBuilder.MapControllerRoute(
            name: "booking-engine-proxy",
            pattern: "engine/{**path}",
            defaults: new { controller = "Api/BookingEngineProxy", action = "Proxy" });
        
        // Map API controllers
        u.EndpointRouteBuilder.MapControllers();
        
        // Register custom routes BEFORE Umbraco website endpoints
        // These MUST come before UseWebsiteEndpoints() to prevent Umbraco from trying to match them as documents
        u.EndpointRouteBuilder.MapControllerRoute(
            name: "hotels",
            pattern: "hotels",
            defaults: new { controller = "Hotel", action = "HotelList" });

        // Event and offer routes - MUST come before hotel-details and UseWebsiteEndpoints
        // These routes need to be matched BEFORE Umbraco's route transformer runs
        u.EndpointRouteBuilder.MapControllerRoute(
            name: "event",
            pattern: "hotels/{hotelSlug}/events/{eventSlug}",
            defaults: new { controller = "Hotel", action = "Event" },
            constraints: new { hotelSlug = @"[^/]+", eventSlug = @"[^/]+" })
            .WithMetadata(new Microsoft.AspNetCore.Routing.RouteNameMetadata("event"));
        
        u.EndpointRouteBuilder.MapControllerRoute(
            name: "offer",
            pattern: "hotels/{hotelSlug}/offers/{offerSlug}",
            defaults: new { controller = "Hotel", action = "Offer" },
            constraints: new { hotelSlug = @"[^/]+", offerSlug = @"[^/]+" })
            .WithMetadata(new Microsoft.AspNetCore.Routing.RouteNameMetadata("offer"));

        u.EndpointRouteBuilder.MapControllerRoute(
            name: "hotel-details",
            pattern: "hotels/{slug}",
            defaults: new { controller = "Hotel", action = "HotelDetails" });

        u.EndpointRouteBuilder.MapControllerRoute(
            name: "hotel-rooms",
            pattern: "hotels/{hotelSlug}/rooms",
            defaults: new { controller = "Hotel", action = "HotelRooms" });

        u.EndpointRouteBuilder.MapControllerRoute(
            name: "room",
            pattern: "hotels/{hotelSlug}/rooms/{roomSlug}",
            defaults: new { controller = "Hotel", action = "Room" });
        
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
