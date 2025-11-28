# Creating Demo Hotel Content

## Quick Start

The demo hotel content can be created via API once content types exist.

## Step 1: Create Content Types in Umbraco Backoffice

1. **Access Umbraco Backoffice**: https://localhost:44372/umbraco
2. Login with your admin credentials
3. Go to **Settings** → **Document Types**

### Create "Hotel" Content Type

1. Click **Create** → **Document Type**
2. **Name**: `Hotel`
3. **Alias**: `hotel` (must be exactly "hotel")
4. **Icon**: Choose `icon-building`
5. **Permissions** tab: Check ✅ **"Allow as root"**
6. **Properties** tab, add these properties:
   - `hotelName` (Textstring) - Mandatory ✅
   - `description` (Textarea)
   - `address` (Textstring)
   - `city` (Textstring)
   - `country` (Textstring)
   - `phone` (Textstring)
   - `email` (Textstring)
7. Click **Save**

### Create "Room" Content Type

1. Click **Create** → **Document Type**
2. **Name**: `Room`
3. **Alias**: `room` (must be exactly "room")
4. **Icon**: Choose `icon-home`
5. **Permissions** tab: 
   - Set **Parent**: Select "Hotel"
6. **Properties** tab, add these properties:
   - `roomName` (Textstring) - Mandatory ✅
   - `description` (Textarea)
   - `maxOccupancy` (Integer)
   - `priceFrom` (Decimal)
   - `roomType` (Textstring)
7. Click **Save**

### Create "Offer" Content Type

1. Click **Create** → **Document Type**
2. **Name**: `Offer`
3. **Alias**: `offer` (must be exactly "offer")
4. **Icon**: Choose `icon-gift`
5. **Permissions** tab:
   - Set **Parent**: Select "Hotel"
6. **Properties** tab, add these properties:
   - `offerName` (Textstring) - Mandatory ✅
   - `description` (Textarea)
   - `discount` (Decimal)
   - `validFrom` (Date Picker with Time)
   - `validTo` (Date Picker with Time)
7. Click **Save**

## Step 2: Create Demo Hotel via API

Once content types are created, call the seed endpoint:

```powershell
# PowerShell
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/create-demo-hotel" -Method POST
```

```bash
# Bash/curl
curl -X POST http://localhost:44372/api/seed/create-demo-hotel
```

Or via browser/postman:
- **URL**: `http://localhost:44372/api/seed/create-demo-hotel`
- **Method**: `POST`

## What Gets Created

The seed endpoint will create:

### Hotel: "Grand Hotel Example"
- Name: Grand Hotel Example
- Description: A luxurious hotel located in the heart of the city
- Address: 123 Main Street
- City: London
- Country: United Kingdom
- Phone: +44 20 1234 5678
- Email: info@grandhotel.example

### 4 Rooms:
1. **Deluxe Double Room** - £150/night, Max 2 guests
2. **Executive Suite** - £300/night, Max 4 guests
3. **Standard Single Room** - £100/night, Max 1 guest
4. **Family Room** - £250/night, Max 5 guests

### 3 Offers:
1. **Early Bird Special** - 20% discount (valid 6 months)
2. **Weekend Getaway** - 15% discount (valid 3 months)
3. **Long Stay Discount** - 25% discount (valid 12 months)

## Step 3: Verify Content

```powershell
# Check hotels API
Invoke-RestMethod -Uri "http://localhost:44372/api/hotels"
```

Should return the "Grand Hotel Example" hotel.

## Troubleshooting

### Error: "Content type 'hotel' does not exist"
- Make sure you created the content types in Umbraco backoffice
- Verify the alias is exactly "hotel" (lowercase, no spaces)

### Error: 404 Not Found
- Make sure Umbraco is running: `docker-compose ps`
- Check logs: `docker-compose logs mydockerproject`

### Content not appearing
- Check Umbraco backoffice → Content section
- Verify content types are created correctly
- Try restarting Umbraco: `docker-compose restart mydockerproject`

