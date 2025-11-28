# Quick Start: Create Content in Umbraco

## Step 1: Create Content Types (Required First)

Go to **Umbraco Backoffice**: https://localhost:44372/umbraco

### Create "Hotel" Document Type:
1. **Settings** → **Document Types** → **Create** → **Document Type**
2. **Name**: `Hotel`
3. **Alias**: `hotel` (must be exactly "hotel")
4. **Permissions** tab: ✅ Check **"Allow as root"**
5. **Properties** tab, add:
   - `hotelName` (Textstring) - Mandatory
   - `description` (Textarea)
   - `address` (Textstring)
   - `city` (Textstring)
   - `country` (Textstring)
   - `phone` (Textstring)
   - `email` (Textstring)
6. **Save**

### Create "Room" Document Type:
1. **Settings** → **Document Types** → **Create** → **Document Type**
2. **Name**: `Room`
3. **Alias**: `room` (must be exactly "room")
4. **Permissions** tab: Set **Parent** = "Hotel"
5. **Properties** tab, add:
   - `roomName` (Textstring) - Mandatory
   - `description` (Textarea)
   - `maxOccupancy` (Integer)
   - `priceFrom` (Decimal)
   - `roomType` (Textstring)
6. **Save**

### Create "Offer" Document Type:
1. **Settings** → **Document Types** → **Create** → **Document Type**
2. **Name**: `Offer`
3. **Alias**: `offer` (must be exactly "offer")
4. **Permissions** tab: Set **Parent** = "Hotel"
5. **Properties** tab, add:
   - `offerName` (Textstring) - Mandatory
   - `description` (Textarea)
   - `discount` (Decimal)
   - `validFrom` (Date Picker with Time)
   - `validTo` (Date Picker with Time)
6. **Save**

## Step 2: Create Demo Content via API

Once content types are created, run:

```powershell
curl -X POST http://localhost:44372/api/seed/create-demo-hotel
```

Or in PowerShell:
```powershell
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/create-demo-hotel" -Method POST
```

This creates:
- **Grand Hotel Example** (hotel)
- 4 rooms (Deluxe Double, Executive Suite, Standard Single, Family Room)
- 3 offers (Early Bird, Weekend Getaway, Long Stay)

## Step 3: View Content

- **Hotel List**: http://localhost:44372/hotels
- **Hotel Details**: http://localhost:44372/hotels/{hotel-id}
- **Room Page**: http://localhost:44372/hotels/{hotel-id}/rooms/{room-id}

## Or Create Content Manually in Backoffice

1. Go to **Content** section
2. Right-click **Content** → **Create** → **Hotel**
3. Fill in hotel details
4. **Save and Publish**
5. Create **Room** and **Offer** nodes as children

