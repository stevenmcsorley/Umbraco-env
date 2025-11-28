WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add controllers for custom routes
builder.Services.AddControllers();

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
        
        // Register custom routes BEFORE Umbraco website endpoints
        // These will be checked before document routing
        u.EndpointRouteBuilder.MapControllerRoute(
            name: "hotels",
            pattern: "hotels",
            defaults: new { controller = "Hotel", action = "HotelList" });

        u.EndpointRouteBuilder.MapControllerRoute(
            name: "hotel-details",
            pattern: "hotels/{id}",
            defaults: new { controller = "Hotel", action = "HotelDetails" });

        u.EndpointRouteBuilder.MapControllerRoute(
            name: "room",
            pattern: "hotels/{hotelId}/rooms/{roomId}",
            defaults: new { controller = "Hotel", action = "Room" });
        
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
