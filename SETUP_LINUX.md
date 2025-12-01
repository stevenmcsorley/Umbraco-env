# Setup Guide for Linux

This guide explains how to set up the Umbraco booking system on a new Linux machine and import all hotels, rooms, events, offers, and pricing data.

## Prerequisites

- Docker and Docker Compose installed
- `curl` or `wget` for making API requests
- `jq` (optional, for pretty JSON output)

## Step 1: Clone and Start Services

```bash
# Clone the repository
git clone <repository-url>
cd UmbraccoEnv

# Navigate to the Docker project
cd MyDockerProject

# Start all services
docker-compose up -d

# Wait for services to be ready (about 30-60 seconds)
# Check logs to ensure services are running
docker-compose logs -f
# Press Ctrl+C when services are ready
```

## Step 2: Create Document Types

The system needs Document Types (Hotel, Room, Event, Offer, AddOn) to be created before importing data.

```bash
# Create all document types at once
curl -X POST http://localhost:44372/api/seed/create-document-types

# Verify it worked (should return success message)
```

**Note:** If you get an error, wait a bit longer for Umbraco to fully initialize, then try again.

## Step 3: Create Database Tables

Before importing data with prices, ensure the Inventory, Bookings, and Users tables exist:

```bash
# Create database tables
curl -X POST http://localhost:44372/api/migration/ensure-tables

# Should return: {"message": "Tables ensured successfully"}
```

## Step 4: Ensure Root Content Exists

The import system requires root content to exist. When you first set up Umbraco, it should create a root content node automatically. If not, you may need to:

1. Go to Umbraco Backoffice → Content
2. Check if there's a root content node (usually a "Home" page or similar)
3. If none exists, create one manually

**Note:** The import will fail with "No root content found" if there's no root content. This is usually created during Umbraco's initial setup.

## Step 5: Import All Data

You have several options for importing data:

### Option A: Import from Existing JSON Files

The repository includes sample import files:
- `sample-import.json` - Basic example
- `test-import-multiple.json` - Comprehensive example with multiple hotels
- `test-import-new-hotels.json` - Additional hotels
- `import-all-prices.json` - Generated prices (if available)

**Import a single file:**
```bash
# Using curl
curl -X POST http://localhost:44372/api/importer/import \
  -H "Content-Type: application/json" \
  -d @- <<EOF
{
  "ContentJson": $(cat MyDockerProject/sample-import.json | jq -c .)
}
EOF

# Or using a simpler approach with file upload endpoint
curl -X POST http://localhost:44372/api/importer/import-file \
  -F "file=@MyDockerProject/sample-import.json"
```

**Import multiple files:**
```bash
# Import comprehensive data
curl -X POST http://localhost:44372/api/importer/import-file \
  -F "file=@MyDockerProject/test-import-multiple.json"

# Import additional hotels
curl -X POST http://localhost:44372/api/importer/import-file \
  -F "file=@MyDockerProject/test-import-new-hotels.json"
```

### Option B: Create Your Own Import File

Create a JSON file with all your hotels, rooms, events, offers, and pricing:

```json
{
  "hotels": [
    {
      "name": "Grand Hotel London",
      "slug": "grand-hotel-london",
      "description": "Luxury hotel in the heart of London",
      "location": "London, United Kingdom",
      "address": "123 Park Lane",
      "city": "London",
      "country": "United Kingdom",
      "rooms": [
        {
          "name": "Presidential Suite",
          "slug": "presidential-suite",
          "description": "Luxurious suite with panoramic views",
          "maxOccupancy": 4,
          "bedType": "King",
          "size": "120 m²",
          "prices": {
            "2025-12-19": 800.00,
            "2025-12-20": 800.00,
            "2025-12-21": 850.00
          },
          "availability": {
            "2025-12-19": 2,
            "2025-12-20": 2,
            "2025-12-21": 1
          }
        }
      ],
      "events": [
        {
          "name": "Live Jazz Night",
          "slug": "live-jazz-night",
          "description": "An evening of live jazz music",
          "eventDate": "2025-12-19T19:00:00Z",
          "location": "Rooftop Bar",
          "price": 45.00,
          "capacity": 50
        }
      ],
      "offers": [
        {
          "name": "Weekend Getaway",
          "slug": "weekend-getaway",
          "description": "Special weekend rates",
          "discount": 15.00,
          "validFrom": "2025-11-29T00:00:00Z",
          "validTo": "2026-02-28T00:00:00Z",
          "requiresWeekend": true
        }
      ]
    }
  ]
}
```

Then import it:
```bash
curl -X POST http://localhost:44372/api/importer/import-file \
  -F "file=@your-import-file.json"
```

### Option C: Generate Prices for Existing Hotels

If you've imported hotels but need to generate prices, you can use the PowerShell script (if you have PowerShell Core installed) or create a similar script in bash.

**Using PowerShell Core (if available):**
```bash
# Install PowerShell Core on Linux if needed
# Then run:
pwsh MyDockerProject/generate-prices.ps1

# This creates import-all-prices.json
# Then import it:
curl -X POST http://localhost:44372/api/importer/import-file \
  -F "file=@MyDockerProject/import-all-prices.json"
```

## Step 6: Verify Import

Check that data was imported successfully:

```bash
# List all hotels
curl http://localhost:44372/api/hotels | jq .

# Get a specific hotel's rooms
curl http://localhost:44372/api/hotels/{hotel-id}/rooms | jq .

# Get events for a hotel
curl http://localhost:44372/api/hotels/{hotel-id}/events | jq .
```

## Complete Setup Script

Here's a complete bash script to set up everything:

```bash
#!/bin/bash

set -e  # Exit on error

echo "=== Setting up Umbraco Booking System ==="

# Step 1: Start services
echo "Starting Docker services..."
cd MyDockerProject
docker-compose up -d

# Wait for services to be ready
echo "Waiting for services to initialize..."
sleep 30

# Step 2: Create document types
echo "Creating document types..."
curl -X POST http://localhost:44372/api/seed/create-document-types || {
    echo "Warning: Document types creation failed. Waiting longer..."
    sleep 30
    curl -X POST http://localhost:44372/api/seed/create-document-types
}

# Step 3: Create database tables
echo "Creating database tables..."
curl -X POST http://localhost:44372/api/migration/ensure-tables

# Step 4: Wait a bit more for root content
echo "Waiting for Umbraco to initialize root content..."
sleep 10

# Step 5: Import data
echo "Importing data..."

# Import comprehensive data
if [ -f "test-import-multiple.json" ]; then
    echo "Importing test-import-multiple.json..."
    curl -X POST http://localhost:44372/api/importer/import-file \
      -F "file=@test-import-multiple.json"
fi

# Import additional hotels if available
if [ -f "test-import-new-hotels.json" ]; then
    echo "Importing test-import-new-hotels.json..."
    curl -X POST http://localhost:44372/api/importer/import-file \
      -F "file=@test-import-new-hotels.json"
fi

# Import prices if available
if [ -f "import-all-prices.json" ]; then
    echo "Importing prices..."
    curl -X POST http://localhost:44372/api/importer/import-file \
      -F "file=@import-all-prices.json"
fi

echo "=== Setup Complete ==="
echo "Access Umbraco Backoffice: https://localhost:44372/umbraco"
echo "View hotels: http://localhost:44372/hotels"
```

Save this as `setup.sh`, make it executable, and run it:
```bash
chmod +x setup.sh
./setup.sh
```

## Troubleshooting

### Services not ready
If you get connection errors, wait longer for services to initialize:
```bash
# Check service status
docker-compose ps

# Check logs
docker-compose logs mydockerproject
```

### Import errors
Check the import response for errors:
```bash
curl -X POST http://localhost:44372/api/importer/import-file \
  -F "file=@MyDockerProject/sample-import.json" | jq .
```

### Missing document types
If document types don't exist, create them manually or wait for the API to be ready:
```bash
# Try again after waiting
sleep 30
curl -X POST http://localhost:44372/api/seed/create-document-types
```

## What Gets Imported

The import system creates/updates **Umbraco content nodes** that appear in the backoffice:

- ✅ **Hotels** - All hotel information (name, description, location, etc.) - Created as root-level content nodes
- ✅ **Rooms** - Room details (name, description, features, etc.) - Created as child content under hotels
- ✅ **Events** - Event information (name, date, location, price, capacity) - Created as child content under hotels
- ✅ **Offers** - Special offers (name, discount, validity period) - Created as child content under hotels
- ✅ **Prices** - Date-specific pricing for rooms (stored in Inventory table)
- ✅ **Availability** - Room availability per date (stored in Inventory table)

**What appears in Umbraco Backoffice:**
- All hotels will appear in the Content tree as root-level items
- Each hotel will have its rooms, events, and offers as child nodes
- You can edit all imported content through the backoffice
- All content is automatically published and visible on the frontend

**What is NOT imported:**
- ❌ **Homepage** - You'll need to create a homepage manually in Umbraco backoffice
- ❌ **Other pages** - Any additional pages (About, Contact, etc.) need to be created manually
- ❌ **Images** - Image URLs in import data are not downloaded; you'll need to upload images manually
- ❌ **User accounts** - User accounts are not imported; create them through registration or backoffice

**Creating a Homepage:**
After importing, you may want to create a homepage:
1. Go to Umbraco Backoffice → Content
2. Right-click on the root → Create
3. Create a "Home" page (or use your homepage document type)
4. Set it as the homepage/start node in Umbraco settings

## Next Steps

After importing:
1. **Upload Images**: Go to Umbraco backoffice → Media → Upload hotel/room images
2. **Verify Data**: Check that hotels, rooms, events, and offers appear correctly
3. **Test Booking**: Try making a booking through the booking engine
4. **Update Prices**: Re-import with updated prices anytime using the same import endpoint

## Updating Data

To update existing hotels, rooms, events, or prices, simply re-import with the same data:
- Hotels with the same name will be **updated** (not duplicated)
- Rooms with the same name under the same hotel will be **updated**
- Prices for existing dates will be **updated**
- New dates will create new inventory entries

```bash
# Re-import to update prices
curl -X POST http://localhost:44372/api/importer/import-file \
  -F "file=@updated-prices.json"
```

