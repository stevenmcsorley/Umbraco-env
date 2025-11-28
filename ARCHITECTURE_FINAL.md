# Final Architecture (Corrected)

## Architecture Overview

### Main Frontend: Razor Components + JavaScript ✅
- **Technology**: Razor views, Razor partials/components
- **Interactivity**: Vanilla JavaScript (no React)
- **Components**: Hero, Gallery, FAQ, Features, Cards, etc.
- **Location**: `MyDockerProject/Views/Partials/`
- **Purpose**: Universal, reusable Razor components for any site type

### React: ONLY for Booking Engine ✅
- **Technology**: React + TypeScript
- **Location**: `frontend/src/booking-engine/`
- **Purpose**: Standalone booking component
- **Integration**: Embedded seamlessly in Razor pages via partial

## Component Structure

### Razor Partials (Universal Components)
Located in `MyDockerProject/MyDockerProject/Views/Partials/`:

- **Hero.cshtml** - Hero section with heading, tagline, image
- **Gallery.cshtml** - Image gallery with JavaScript lightbox
- **FAQ.cshtml** - Accordion FAQ section with JavaScript toggle
- **Features.cshtml** - Feature grid display
- **Cards.cshtml** - Card grid (configurable columns)
- **BookingEngine.cshtml** - Wrapper that embeds React booking engine seamlessly

### How Components Work

1. **Razor Components**: Use Razor syntax + JavaScript for interactivity
   - Example: Gallery uses JavaScript for lightbox functionality
   - Example: FAQ uses JavaScript for accordion behavior

2. **React Booking Engine**: Loaded via script tag
   - Razor `BookingEngine.cshtml` partial loads React component
   - Seamless integration - looks like part of Razor page

## Usage in Razor Views

```razor
@* Example: Hotel page using components *@
@{
    Layout = "Layouts/Main.cshtml";
}

@await Html.PartialAsync("Partials/Hero", new {
    heading = "Grand Hotel",
    tagline = "Luxury accommodation",
    image = "/media/hotel-hero.jpg"
})

@await Html.PartialAsync("Partials/Features", new {
    title = "Hotel Features",
    items = new[] {
        new { title = "WiFi", description = "Free WiFi", icon = "📶" },
        new { title = "Pool", description = "Swimming pool", icon = "🏊" }
    }
})

@await Html.PartialAsync("Partials/Gallery", new {
    title = "Photo Gallery",
    images = new[] { "/media/img1.jpg", "/media/img2.jpg" }
})

@* Booking engine embedded seamlessly *@
@await Html.PartialAsync("Partials/BookingEngine", new {
    productId = "room-123",
    hotelId = "hotel-456",
    apiBaseUrl = "/engine"
})
```

## Benefits

✅ **Universal**: Razor components work for hotels, venues, stadiums, events, etc.
✅ **Manageable**: Content editors manage via Umbraco Block Grid
✅ **Interactive**: JavaScript adds interactivity without React overhead
✅ **Seamless**: React booking engine integrates perfectly
✅ **Flexible**: Easy to add new Razor components

## Summary

- **Razor Components** = Main site components (Hero, Gallery, FAQ, etc.)
- **JavaScript** = Adds interactivity to Razor components
- **React** = ONLY for booking engine
- **Integration** = Seamless via Razor partials

