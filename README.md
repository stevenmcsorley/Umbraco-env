# Umbraco Site Kit with Booking Engine

Universal, reusable website-building system for Umbraco CMS with React-based Booking Engine.

## Architecture

### Main Frontend: Razor Components + JavaScript ✅
- **Technology**: Razor views, Razor partials/components
- **Interactivity**: Vanilla JavaScript (no React)
- **Components**: Hero, Gallery, FAQ, Features, Cards, etc.
- **Location**: `MyDockerProject/Views/Partials/` 
- **Purpose**: Universal, reusable Razor components for any site type (hotels, venues, stadiums, events, etc.)

### React: ONLY for Booking Engine ✅
- **Technology**: React + TypeScript
- **Location**: `frontend/src/booking-engine/`
- **Purpose**: Standalone booking component
- **Integration**: Embedded seamlessly in Razor pages via `BookingEngine.cshtml` partial

## Project Structure

```
├── MyDockerProject/              # Umbraco CMS project (Docker)
│   ├── engine/                   # Booking Engine Backend (Node.js + TypeScript, Docker)
│   ├── MyDockerProject/
│   │   ├── Views/
│   │   │   ├── Partials/         # Universal Razor components
│   │   │   │   ├── Hero.cshtml
│   │   │   │   ├── Gallery.cshtml
│   │   │   │   ├── FAQ.cshtml
│   │   │   │   ├── Features.cshtml
│   │   │   │   ├── Cards.cshtml
│   │   │   │   └── BookingEngine.cshtml  # Embeds React booking engine
│   │   │   ├── hotelList.cshtml  # Hotel list page
│   │   │   ├── hotel.cshtml      # Hotel details page
│   │   │   └── room.cshtml       # Room page with booking
│   │   ├── Controllers/
│   │   │   ├── Api/              # API endpoints
│   │   │   └── HotelController.cs
│   │   ├── Services/             # Business logic
│   │   └── wwwroot/scripts/      # Built React booking engine (booking-engine.js)
│   └── docker-compose.yml
├── frontend/                      # React Booking Engine source (build separately)
│   └── src/booking-engine/       # React booking engine component
└── docs/                          # Documentation
```

## Prerequisites

- Docker and Docker Compose
- Node.js 20+ (for local development)
- .NET 9 SDK (for local Umbraco development)

## Getting Started

### 1. Start All Services (Docker)

```bash
cd MyDockerProject
docker-compose up -d
```

This starts:
- SQL Server database (port 1433)
- Umbraco CMS (port 44372)
- Booking Engine Backend (port 3001)

**All services run in Docker - no local processes needed!**

**Note**: The React booking engine must be built separately and copied to `wwwroot/scripts/` before starting Docker. See [BUILD_BOOKING_ENGINE.md](BUILD_BOOKING_ENGINE.md) for instructions.

### 2. Access Services

- **Umbraco Backoffice**: https://localhost:44372/umbraco
- **Umbraco API**: http://localhost:44372/api/hotels
- **Booking Engine API**: http://localhost:3001/health
- **React Booking Engine**: Served from Umbraco at `/scripts/booking-engine.js`

### 3. Create Content Types

1. Go to Umbraco Backoffice: https://localhost:44372/umbraco
2. Navigate to **Settings** → **Document Types**
3. Create:
   - **Hotel** (alias: `hotel`, allowed as root)
   - **Room** (alias: `room`, child of Hotel)
   - **Offer** (alias: `offer`, child of Hotel)
   - **Event** (alias: `event`, child of Hotel) - optional

See [Getting Started Guide](docs/GETTING_STARTED.md) for detailed instructions.

### 4. Seed Demo Content

Once content types exist:

```bash
curl -X POST http://localhost:44372/api/seed/create-demo-hotel
```

This creates:
- "Grand Hotel Example" hotel
- 4 example rooms
- 3 example offers

### 5. Import Data (Bulk Import)

You can bulk import hotels, rooms, events, and offers using the import API:

**Using JSON in request body:**
```powershell
$jsonContent = Get-Content -Path "sample-import.json" -Raw
$body = @{ ContentJson = $jsonContent } | ConvertTo-Json -Depth 10
Invoke-RestMethod -Uri "http://localhost:44372/api/importer/import" -Method POST -Body $body -ContentType "application/json"
```

**Using file upload:**
```powershell
$filePath = "sample-import.json"
$form = @{
    file = Get-Item $filePath
}
Invoke-RestMethod -Uri "http://localhost:44372/api/importer/import-file" -Method POST -Form $form
```

**Import JSON Format:**
```json
{
  "hotels": [
    {
      "name": "Hotel Name",
      "slug": "hotel-slug",
      "description": "Hotel description",
      "address": "123 Street",
      "city": "City",
      "country": "Country",
      "rooms": [
        {
          "name": "Room Name",
          "slug": "room-slug",
          "description": "Room description",
          "maxOccupancy": 2,
          "bedType": "King",
          "size": "50 m²",
          "prices": {
            "2025-12-19": 200.00,
            "2025-12-20": 200.00
          },
          "availability": {
            "2025-12-19": 5,
            "2025-12-20": 5
          }
        }
      ],
      "events": [
        {
          "name": "Event Name",
          "slug": "event-slug",
          "description": "Event description",
          "eventDate": "2025-12-19T19:00:00Z",
          "location": "Event Location",
          "price": 50.00,
          "capacity": 100
        }
      ],
      "offers": [
        {
          "name": "Offer Name",
          "slug": "offer-slug",
          "description": "Offer description",
          "discount": 15.00,
          "validFrom": "2025-12-01T00:00:00Z",
          "validTo": "2026-12-31T00:00:00Z"
        }
      ]
    }
  ]
}
```

See `sample-import.json` and `test-import-multiple.json` for complete examples.

**Note:** The import creates content as published. If hotels already exist (by name), they will be skipped to avoid duplicates.

### 6. View Pages

- **Hotel List**: http://localhost:44372/hotels
- **Hotel Details**: http://localhost:44372/hotels/{id}
- **Room Page**: http://localhost:44372/hotels/{hotelId}/rooms/{roomId}

## Razor Components

Universal components available in `Views/Partials/`:

### Hero
```razor
@await Html.PartialAsync("Partials/Hero", new {
    heading = "Welcome",
    tagline = "Your tagline here",
    image = "/media/hero.jpg",
    backgroundColor = "bg-blue-900",
    textColor = "text-white"
})
```

### Gallery
```razor
@await Html.PartialAsync("Partials/Gallery", new {
    title = "Photo Gallery",
    images = new[] { "/media/img1.jpg", "/media/img2.jpg" }
})
```

### FAQ
```razor
@await Html.PartialAsync("Partials/FAQ", new {
    title = "Frequently Asked Questions",
    items = new[] {
        new { question = "Question 1?", answer = "Answer 1" },
        new { question = "Question 2?", answer = "Answer 2" }
    }
})
```

### Features
```razor
@await Html.PartialAsync("Partials/Features", new {
    title = "Features",
    items = new[] {
        new { title = "Feature 1", description = "Description", icon = "⭐" },
        new { title = "Feature 2", description = "Description", icon = "🏊" }
    }
})
```

### Cards
```razor
@await Html.PartialAsync("Partials/Cards", new {
    title = "Our Services",
    columns = 3,
    items = new[] {
        new { title = "Card 1", description = "Description", image = "/media/card1.jpg", link = "/service1" }
    }
})
```

### Booking Engine
```razor
@await Html.PartialAsync("Partials/BookingEngine", new {
    productId = "room-123",
    hotelId = "hotel-456",
    apiBaseUrl = "/engine"
})
```

## API Endpoints

### Umbraco CMS APIs

- `GET /api/hotels` - List all hotels
- `GET /api/hotels/{id}` - Get hotel details
- `GET /api/hotels/{id}/rooms` - Get rooms for a hotel
- `GET /api/hotels/{id}/offers` - Get offers for a hotel
- `GET /api/hotels/{id}/availability` - Get basic availability structure
- `POST /api/importer/import` - Bulk import hotels, rooms, events, and offers from JSON
- `POST /api/importer/import-file` - Bulk import from uploaded JSON file
- `POST /api/seed/create-demo-hotel` - Create demo hotel content

### Booking Engine APIs

- `GET /engine/availability?productId={id}&from={date}&to={date}` - Check availability
- `POST /engine/book` - Create booking
- `GET /engine/health` - Health check

## Development

### Local Development (Optional)

For React booking engine development with hot-reload:

1. Start Umbraco + Database + Booking Engine: `docker-compose up -d` in `MyDockerProject/`
2. Start React dev server: `cd frontend && npm run dev`
3. Update CSHTML files to use `http://localhost:5173/booking-engine.js` during development
4. Remember to build and copy to `wwwroot/scripts/` for production

### Rebuilding Services

```bash
# Rebuild specific service
docker-compose up -d --build mydockerproject

# Rebuild all
docker-compose up -d --build
```

### Viewing Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f mydockerproject
docker-compose logs -f booking_engine
```

## Universal Components

All Razor components are **universal** and work for:
- Hotels
- Event venues
- Stadiums & arenas
- Attractions
- Multi-location businesses
- Tourism boards
- Any structured-content domain

Components use vanilla JavaScript for interactivity (lightbox, accordion, etc.) - no React overhead for main site components.

## Umbraco Site Kit - Programmatic Content Type Creation

The Umbraco Site Kit allows you to programmatically create Document Types and Element Types with their properties via API endpoints, eliminating manual setup in the Umbraco backoffice.

**📖 For complete documentation, see [Umbraco Site Kit Guide](docs/UMBRACO_SITE_KIT.md)**

### Quick Start - Create Element Types

```powershell
# Create all 14 Element Types
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/create-partial-element-types" -Method POST

# Add properties to Element Types
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/add-properties-to-element-types" -Method POST
```

### Available Element Types

The system creates 14 reusable Element Types:
- Hero, Gallery, Features, FAQ, Cards, CTA Panel, Events, Offers, Rooms, Rich Text, Testimonials, Accordion, Tabs, Map

Each Element Type has properties automatically assigned to the "content" group for easy editing in Umbraco backoffice.

### API Endpoints

- `POST /api/seed/create-partial-element-types` - Create all Element Types
- `POST /api/seed/add-properties-to-element-types` - Add properties to Element Types
- `GET /api/seed/list-element-types` - List all Element Types
- `POST /api/seed/create-document-types` - Create Document Types (Hotel, Room, Offer)
- `POST /api/seed/add-hotel-properties` - Add properties to Hotel Document Type

See **[docs/UMBRACO_SITE_KIT.md](docs/UMBRACO_SITE_KIT.md)** for complete API reference, usage examples, troubleshooting, and best practices.

## Data Import System

The system includes a comprehensive data import feature that allows you to bulk import hotels, rooms, events, and offers from JSON files.

**Features:**
- ✅ Import multiple hotels in a single request
- ✅ Import rooms with pricing and availability data
- ✅ Import events with capacity and pricing
- ✅ Import offers with discount and validity periods
- ✅ Automatic duplicate detection (skips existing hotels by name)
- ✅ Creates and publishes content automatically
- ✅ Detailed import results with success/error reporting

**Sample Files:**
- `sample-import.json` - Basic example with one hotel
- `test-import-multiple.json` - Comprehensive example with 4 hotels, multiple rooms, events, and offers

**Import Response:**
```json
{
  "success": true,
  "message": "Import completed. Created: 4 hotels, 9 rooms, 10 events, 8 offers...",
  "hotelsCreated": 4,
  "roomsCreated": 9,
  "eventsCreated": 10,
  "offersCreated": 8,
  "inventoryEntriesCreated": 0,
  "errors": []
}
```

**Note:** Inventory/pricing data requires the Inventory database table to be created. See [Inventory and Import System](docs/INVENTORY_AND_IMPORT_SYSTEM.md) for database setup.

## Documentation

- **[Getting Started Guide](docs/GETTING_STARTED.md)** - Quick start and setup instructions
- **[Umbraco Site Kit Guide](docs/UMBRACO_SITE_KIT.md)** ⭐ - Complete guide to programmatic Document Type and Element Type creation (API endpoints, usage examples, troubleshooting)
- **[Inventory and Import System](docs/INVENTORY_AND_IMPORT_SYSTEM.md)** - Data import system documentation and database setup
- **[Architecture Documentation](docs/architecture.md)** - System architecture overview
- **[Booking Engine Implementation](BOOKING_ENGINE_IMPLEMENTATION.md)** - Booking engine details and features
- **[Build Booking Engine](BUILD_BOOKING_ENGINE.md)** - React booking engine build instructions

## Summary

✅ **Main Frontend**: Razor components + JavaScript (universal, reusable)
✅ **React**: ONLY for booking engine (embeddable)
✅ **Integration**: Seamless via Razor partials
✅ **Data Import**: Bulk import hotels, rooms, events, and offers via JSON
✅ **All Services**: Running in Docker
