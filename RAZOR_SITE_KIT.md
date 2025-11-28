# Razor Site Kit - Universal Component Library

## Overview

Complete Razor component library for building universal websites. All components use Razor syntax + vanilla JavaScript for interactivity.

## Components

### ✅ Hero
**File**: `Views/Partials/Hero.cshtml`

Hero section with heading, tagline, and image.

```razor
@await Html.PartialAsync("Partials/Hero", new {
    heading = "Welcome",
    tagline = "Your tagline here",
    image = "/media/hero.jpg",
    backgroundColor = "bg-blue-900",
    textColor = "text-white"
})
```

### ✅ Gallery
**File**: `Views/Partials/Gallery.cshtml`

Image gallery with JavaScript lightbox functionality.

```razor
@await Html.PartialAsync("Partials/Gallery", new {
    title = "Photo Gallery",
    images = new[] { "/media/img1.jpg", "/media/img2.jpg" }
})
```

### ✅ FAQ
**File**: `Views/Partials/FAQ.cshtml`

Accordion-style FAQ section with JavaScript toggle.

```razor
@await Html.PartialAsync("Partials/FAQ", new {
    title = "Frequently Asked Questions",
    items = new[] {
        new { question = "Question 1?", answer = "Answer 1" },
        new { question = "Question 2?", answer = "Answer 2" }
    }
})
```

### ✅ Features
**File**: `Views/Partials/Features.cshtml`

Feature grid display.

```razor
@await Html.PartialAsync("Partials/Features", new {
    title = "Features",
    items = new[] {
        new { title = "Feature 1", description = "Description", icon = "⭐" },
        new { title = "Feature 2", description = "Description", icon = "🏊" }
    }
})
```

### ✅ Cards
**File**: `Views/Partials/Cards.cshtml`

Card grid with configurable columns.

```razor
@await Html.PartialAsync("Partials/Cards", new {
    title = "Our Services",
    columns = 3,
    items = new[] {
        new { title = "Card 1", description = "Description", image = "/media/card1.jpg", link = "/service1" }
    }
})
```

### ✅ Rich Text
**File**: `Views/Partials/RichText.cshtml`

Rich text content display.

```razor
@await Html.PartialAsync("Partials/RichText", new {
    content = "<p>Your HTML content here</p>",
    className = "py-8"
})
```

### ✅ Offers
**File**: `Views/Partials/Offers.cshtml`

Special offers display grid.

```razor
@await Html.PartialAsync("Partials/Offers", new {
    title = "Special Offers",
    items = new[] {
        new { name = "Early Bird", description = "Save 20%", discount = 20, image = "/media/offer1.jpg", validFrom = "2025-01-01", validTo = "2025-12-31", link = "/offers/early-bird" }
    }
})
```

### ✅ Rooms
**File**: `Views/Partials/Rooms.cshtml`

Room listing grid with booking links.

```razor
@await Html.PartialAsync("Partials/Rooms", new {
    title = "Our Rooms",
    hotelId = "hotel-123",
    items = new[] {
        new { id = "room-1", name = "Deluxe Room", description = "Spacious room", priceFrom = 150, maxOccupancy = 2, image = "/media/room1.jpg" }
    }
})
```

### ✅ Events
**File**: `Views/Partials/Events.cshtml`

Event listing grid.

```razor
@await Html.PartialAsync("Partials/Events", new {
    title = "Upcoming Events",
    items = new[] {
        new { name = "Concert", description = "Live music", eventDate = "2025-06-15", price = 50, image = "/media/event1.jpg", link = "/events/concert" }
    }
})
```

### ✅ Testimonials
**File**: `Views/Partials/Testimonials.cshtml`

Customer testimonials grid.

```razor
@await Html.PartialAsync("Partials/Testimonials", new {
    title = "What Our Customers Say",
    items = new[] {
        new { quote = "Great experience!", name = "John Doe", role = "Guest", rating = 5, avatar = "/media/avatar1.jpg" }
    }
})
```

### ✅ CTA Panel
**File**: `Views/Partials/CTAPanel.cshtml`

Call-to-action panel.

```razor
@await Html.PartialAsync("Partials/CTAPanel", new {
    heading = "Ready to Get Started?",
    text = "Join us today",
    buttonText = "Sign Up",
    buttonLink = "/signup",
    backgroundColor = "bg-blue-600",
    textColor = "text-white"
})
```

### ✅ Accordion
**File**: `Views/Partials/Accordion.cshtml`

Generic accordion component (separate from FAQ).

```razor
@await Html.PartialAsync("Partials/Accordion", new {
    title = "More Information",
    items = new[] {
        new { title = "Section 1", content = "<p>Content 1</p>" },
        new { title = "Section 2", content = "<p>Content 2</p>" }
    }
})
```

### ✅ Tabs
**File**: `Views/Partials/Tabs.cshtml`

Tabbed content interface.

```razor
@await Html.PartialAsync("Partials/Tabs", new {
    defaultTab = 0,
    items = new[] {
        new { label = "Tab 1", content = "<p>Content for tab 1</p>" },
        new { label = "Tab 2", content = "<p>Content for tab 2</p>" }
    }
})
```

### ✅ Map
**File**: `Views/Partials/Map.cshtml`

Map display (supports coordinates or address).

```razor
@await Html.PartialAsync("Partials/Map", new {
    address = "123 Main St, London",
    latitude = "51.5074",
    longitude = "-0.1278",
    height = "400px"
})
```

### ✅ Booking Engine
**File**: `Views/Partials/BookingEngine.cshtml`

Wrapper that embeds React booking engine seamlessly.

```razor
@await Html.PartialAsync("Partials/BookingEngine", new {
    productId = "room-123",
    hotelId = "hotel-456",
    apiBaseUrl = "/engine"
})
```

## Universal Design

All components are **universal** and work for:
- ✅ Hotels
- ✅ Event venues
- ✅ Stadiums & arenas
- ✅ Attractions
- ✅ Multi-location businesses
- ✅ Tourism boards
- ✅ Any structured-content domain

## Technology

- **Razor Syntax**: Server-side rendering
- **JavaScript**: Vanilla JS for interactivity (lightbox, accordion, tabs)
- **Styling**: Tailwind CSS (already configured)
- **No React**: Main site uses zero React overhead

## Usage

All components are Razor partials located in `Views/Partials/`. Use them in any Razor view:

```razor
@{
    Layout = "Layouts/Main.cshtml";
}

@await Html.PartialAsync("Partials/Hero", new { ... })
@await Html.PartialAsync("Partials/Gallery", new { ... })
@await Html.PartialAsync("Partials/BookingEngine", new { ... })
```

## Summary

✅ **Complete Razor Site Kit** with 14 universal components
✅ **JavaScript interactivity** (no React overhead)
✅ **Works for any site type** (hotels, venues, stadiums, events, etc.)
✅ **Seamless React booking engine integration**

