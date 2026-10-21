using Microsoft.EntityFrameworkCore;
using MyDockerProject.Data;
using MyDockerProject.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using MyDockerProject.Models;
using MyDockerProject.Security;
using SiteKit.Core.Components;
using SiteKit.Core.Projects;
using SiteKit.Core.Data;
using SiteKit.Core.Modules;
using SiteKit.Core.Importing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Docker deployments can point ASP.NET Core's key ring at a persistent volume.
// Keep the default host behavior for local development and non-Docker hosts.
var dataProtectionKeyPath = builder.Configuration["SiteKit:DataProtectionKeyPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath))
{
    var keyDirectory = new DirectoryInfo(dataProtectionKeyPath);
    keyDirectory.Create();
    builder.Services.AddDataProtection().PersistKeysToFileSystem(keyDirectory);
}

// Add controllers for custom routes
builder.Services.AddControllers();
builder.Services.AddSingleton<ISiteKitComponentRegistry>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitComponentRegistry(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "components"));
});
builder.Services.AddSingleton<ISiteKitProjectRegistry>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitProjectRegistry(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "projects"));
});
builder.Services.AddSingleton<ISiteKitPlacementRegistry>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitPlacementRegistry(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "placements"));
});
builder.Services.AddSingleton<ISiteKitEntityRegistry>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitEntityRegistry(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "entities"));
});
builder.Services.AddSingleton<ISiteKitRecordStore>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitRecordStore(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "records"));
});
builder.Services.AddSingleton<ISiteKitPermissionRegistry>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitPermissionRegistry(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "permissions"));
});
builder.Services.AddSingleton<ISiteKitProjectRoleRegistry>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitProjectRoleRegistry(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "roles"));
});
builder.Services.AddSingleton<SiteKitPermissionEvaluator>();
builder.Services.AddSingleton<ISiteKitScreenRegistry>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitScreenRegistry(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "screens"));
});
builder.Services.AddSingleton<ISiteKitModuleRegistry>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitModuleRegistry(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "modules"));
});
builder.Services.AddSingleton<ISiteKitImportMappingRegistry>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitImportMappingRegistry(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "imports"));
});
builder.Services.AddSingleton<ISiteKitImportRunRegistry>(services =>
{
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    return new JsonSiteKitImportRunRegistry(Path.Combine(environment.ContentRootPath, "umbraco", "Data", "SiteKit", "import-runs"));
});
builder.Services.AddScoped<SiteKitDataImportService>();

// Add session support for authentication
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add HttpClient for booking engine proxy
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("SiteKitImportSource", client => client.Timeout = TimeSpan.FromSeconds(60))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        ConnectTimeout = TimeSpan.FromSeconds(10),
        MaxResponseHeadersLength = 32
    });

// Add Entity Framework for bookings and inventory
var connectionString = builder.Configuration.GetConnectionString("umbracoDbDSN");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<BookingDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddScoped<ISiteKitRecordStore, SqlSiteKitRecordStore>();
    
    // Register services
    builder.Services.AddScoped<BookingService>();
    builder.Services.AddScoped<InventoryService>();
    builder.Services.AddScoped<DataImportService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<PasswordHasher<User>>();
    
    // Register payment service (can be swapped for real provider)
    var paymentProvider = builder.Configuration["Payment:Provider"] ?? "Mock";
    if (paymentProvider.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddScoped<IPaymentService, StripePaymentService>();
    }
    else
    {
        builder.Services.AddScoped<IPaymentService, MockPaymentService>();
    }
}

// Configure forwarded headers for Cloudflare/proxy
// Cloudflare uses these headers, so we need to trust all proxies
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (var proxyAddress in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        if (System.Net.IPAddress.TryParse(proxyAddress, out var proxyIp))
        {
            options.KnownProxies.Add(proxyIp);
        }
    }
});

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

// Use forwarded headers FIRST (before Umbraco boot) so it knows about HTTPS from Cloudflare
app.UseForwardedHeaders();

// Redirect direct HTTP requests to HTTPS after applying trusted proxy headers.
// In the local Docker HTTPS overlay this prevents OpenIddict from receiving
// authorization/discovery requests over the HTTP-only development port.
app.UseHttpsRedirection();

await app.BootUmbracoAsync();

// Use session middleware
app.UseSession();

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
        
        // Register auth routes BEFORE MapControllers to ensure they take precedence
        // These use route attributes [HttpGet("/logout")] etc, but we register explicit routes to be safe
        u.EndpointRouteBuilder.MapControllerRoute(
            name: "logout",
            pattern: "logout",
            defaults: new { controller = "Auth", action = "Logout" });
        
        u.EndpointRouteBuilder.MapControllerRoute(
            name: "login",
            pattern: "login",
            defaults: new { controller = "Auth", action = "Login" });
        
        u.EndpointRouteBuilder.MapControllerRoute(
            name: "register",
            pattern: "register",
            defaults: new { controller = "Auth", action = "Register" });
        
        // Map API controllers (this will also register regular controllers with route attributes)
        u.EndpointRouteBuilder.MapControllers();
        
        // Register custom routes BEFORE Umbraco website endpoints
        // These MUST come before UseWebsiteEndpoints() to prevent Umbraco from trying to match them as documents
        
        u.EndpointRouteBuilder.MapControllerRoute(
            name: "book",
            pattern: "book",
            defaults: new { controller = "Booking", action = "Book" });
        
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
        
        u.EndpointRouteBuilder.MapControllerRoute(
            name: "account",
            pattern: "account",
            defaults: new { controller = "Account", action = "Dashboard" });
        
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
