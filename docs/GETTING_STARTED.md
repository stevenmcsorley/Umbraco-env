# Getting Started Guide

## Quick Start

### 1. Build React Booking Engine

```bash
cd frontend
npm install
npm run build
# Copy built file to wwwroot
mkdir -p ../MyDockerProject/MyDockerProject/wwwroot/scripts
cp dist/booking-engine.iife.js ../MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js
```

Or use the build script:
```bash
cd MyDockerProject
chmod +x build-booking-engine.sh
./build-booking-engine.sh
```

### 2. Start Docker Services

```bash
cd MyDockerProject
docker-compose up -d
```

This starts:
- SQL Server database (port 1433)
- Umbraco CMS (port 44372)
- Booking Engine Backend (port 3001)

### 3. Access Services

- **Umbraco Backoffice**: https://localhost:44372/umbraco
- **Umbraco API**: http://localhost:44372/api/hotels
- **Booking Engine API**: http://localhost:3001/health

### 4. Create Content Types

Use the Umbraco Site Kit API to create content types programmatically:

```powershell
# Create Element Types
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/create-partial-element-types" -Method POST

# Add properties to Element Types
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/add-properties-to-element-types" -Method POST

# Create Document Types (Hotel, Room, Offer)
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/create-document-types" -Method POST

# Add properties to Hotel
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/add-hotel-properties" -Method POST
```

### 5. Create Demo Content

```bash
curl -X POST http://localhost:44372/api/seed/create-demo-hotel
```

### 6. View Pages

- **Hotel List**: http://localhost:44372/hotels
- **Hotel Details**: http://localhost:44372/hotels/{slug}
- **Room Page**: http://localhost:44372/hotels/{hotelSlug}/rooms/{roomSlug}

## Development

### Rebuilding React Booking Engine

After making changes to the React booking engine:

```bash
cd frontend
npm run build
cp dist/booking-engine.iife.js ../MyDockerProject/MyDockerProject/wwwroot/scripts/booking-engine.js
```

### Rebuilding Docker Services

```bash
cd MyDockerProject
docker-compose up -d --build
```

### Viewing Logs

```bash
docker-compose logs -f mydockerproject
docker-compose logs -f booking_engine
```

## Project Structure

```
├── MyDockerProject/              # Umbraco CMS project (Docker)
│   ├── engine/                    # Booking Engine Backend (Node.js)
│   ├── MyDockerProject/
│   │   ├── Views/                 # Razor views and partials
│   │   ├── Controllers/           # API and page controllers
│   │   ├── Services/              # Business logic
│   │   └── wwwroot/scripts/       # Built React booking engine
│   └── docker-compose.yml
├── frontend/                       # React Booking Engine source (build separately)
│   └── src/booking-engine/
└── docs/                          # Documentation
```

## Documentation

- **[README.md](../README.md)** - Main project documentation
- **[Umbraco Site Kit Guide](UMBRACO_SITE_KIT.md)** ⭐ - Complete guide to programmatic Document Type and Element Type creation
- **[Architecture](architecture.md)** - System architecture
- **[Booking Engine Implementation](../BOOKING_ENGINE_IMPLEMENTATION.md)** - Booking engine details
- **[Build Booking Engine](../BUILD_BOOKING_ENGINE.md)** - React build instructions

