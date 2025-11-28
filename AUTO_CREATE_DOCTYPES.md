# Automatically Create Document Types

You can now create document types programmatically via API instead of manually in the Umbraco backoffice!

## Prerequisites

**Important**: Umbraco comes with default data types, but if you get an error about missing data types, you may need to create them first:

1. Go to **Settings** → **Data Types** in Umbraco backoffice
2. Ensure these data types exist (they usually do by default):
   - A data type using **Textbox** editor (for text fields)
   - A data type using **Textarea** editor (for description fields)
   - A data type using **Integer** editor (for maxOccupancy)
   - A data type using **Decimal** editor (for prices/discounts)
   - A data type using **DateTime** editor (for date fields)

## Quick Start

### Step 0: Check Data Types Status

First, check which data types exist:

```bash
curl http://localhost:44372/api/seed/check-datatypes
```

This will show you which data types are missing. Most Umbraco installations come with default data types, but you may need to create a few manually.

### Step 1: Create Document Types via API

```bash
curl -X POST http://localhost:44372/api/seed/create-document-types
```

Or in PowerShell:
```powershell
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/create-document-types" -Method POST
```

This will create:
- **Hotel** document type (with properties: hotelName, description, address, city, country, phone, email)
- **Room** document type (with properties: roomName, description, maxOccupancy, priceFrom, roomType)
- **Offer** document type (with properties: offerName, description, discount, validFrom, validTo)

### Step 2: Create Demo Content

Once document types are created, create demo content:

```bash
curl -X POST http://localhost:44372/api/seed/create-demo-hotel
```

## What Gets Created

### Hotel Document Type
- **Alias**: `hotel`
- **Allowed as root**: Yes
- **Properties**:
  - `hotelName` (Textstring) - Mandatory
  - `description` (Textarea)
  - `address` (Textstring)
  - `city` (Textstring)
  - `country` (Textstring)
  - `phone` (Textstring)
  - `email` (Textstring)

### Room Document Type
- **Alias**: `room`
- **Parent**: Hotel
- **Properties**:
  - `roomName` (Textstring) - Mandatory
  - `description` (Textarea)
  - `maxOccupancy` (Integer)
  - `priceFrom` (Decimal)
  - `roomType` (Textstring)

### Offer Document Type
- **Alias**: `offer`
- **Parent**: Hotel
- **Properties**:
  - `offerName` (Textstring) - Mandatory
  - `description` (Textarea)
  - `discount` (Decimal)
  - `validFrom` (DateTime)
  - `validTo` (DateTime)

## Notes

- The endpoint is **idempotent** - it won't create duplicates if document types already exist
- Document types are created in the correct order (Hotel first, then Room/Offer as children)
- If document types already exist, the endpoint will skip creation and return success
- **Data types must exist first** - use `/api/seed/check-datatypes` to verify

## Related Endpoints

- `GET /api/seed/check-datatypes` - Check which data types exist and which are missing
- `POST /api/seed/create-document-types` - Create Hotel, Room, Offer document types
- `POST /api/seed/create-demo-hotel` - Create demo hotel content (requires document types)

See `DATATYPES_FOR_PARTIALS.md` for a complete list of data types needed for all Site Kit partials.

## Alternative: Manual Creation

If you prefer to create document types manually:
1. Go to **Settings** → **Document Types** in Umbraco backoffice
2. Follow the steps in `QUICK_START_CONTENT.md`

