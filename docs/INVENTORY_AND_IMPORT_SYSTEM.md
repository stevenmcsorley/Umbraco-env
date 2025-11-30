# Inventory and Data Import System

## Overview

This system provides:
1. **Inventory Management**: Track room availability and event seat capacity by date
2. **Booking Persistence**: Store bookings in database with inventory updates
3. **Data Import**: Bulk import hotels, rooms, events, offers, prices, and availability via JSON

## Database Schema

### Bookings Table
- Stores all bookings with guest details, dates, pricing
- Automatically updates inventory when bookings are created/cancelled
- Supports both rooms (date ranges) and events (single dates)

### Inventory Table
- Tracks availability by product (room/event) and date
- Stores total quantity, booked quantity, and price per date
- Unique constraint: one entry per product per date

## API Endpoints

### Import Data
```
POST /api/importer/import
Content-Type: application/json

{
  "contentJson": "{ ... }"
}
```

Or upload a file:
```
POST /api/importer/import-file
Content-Type: multipart/form-data
file: <json-file>
```

### Create Booking
```
POST /api/bookings
Content-Type: application/json

{
  "productId": "guid",
  "productType": "Room" | "Event",
  "checkIn": "2025-12-19T00:00:00Z",
  "checkOut": "2025-12-21T00:00:00Z", // null for events
  "quantity": 1,
  "guestFirstName": "John",
  "guestLastName": "Doe",
  "guestEmail": "john@example.com",
  "guestPhone": "+1234567890",
  "totalPrice": 1050.00,
  "currency": "GBP",
  "additionalData": "{ ... }", // JSON string for add-ons, events, offers
  "userId": "guid" // optional, for authenticated users
}
```

### Get Booking
```
GET /api/bookings/{bookingReference}
```

### Cancel Booking
```
POST /api/bookings/{bookingReference}/cancel
```

### Get User Bookings
```
GET /api/bookings/user/{userId}
```

## Import JSON Format

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
      "imageUrl": "https://example.com/hotel.jpg",
      "rooms": [
        {
          "name": "Presidential Suite",
          "slug": "presidential-suite",
          "description": "Luxurious suite with panoramic views",
          "maxOccupancy": 4,
          "bedType": "King",
          "size": "120 m²",
          "imageUrl": "https://example.com/suite.jpg",
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
          "capacity": 50,
          "imageUrl": "https://example.com/jazz.jpg"
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

## Setup Instructions

### 1. Run Database Migration

Execute the migration script to create the tables:
```sql
-- Run: MyDockerProject/Database/migrations/001_CreateBookingsAndInventory.sql
```

Or use Entity Framework migrations (if configured):
```bash
dotnet ef migrations add CreateBookingsAndInventory
dotnet ef database update
```

### 2. Configure Connection String

Ensure your `appsettings.json` or Docker environment has the correct connection string:
```json
{
  "ConnectionStrings": {
    "umbracoDbDSN": "Server=umb_database;Database=umbracoDb;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
  }
}
```

### 3. Import Data

Use the import endpoint to bulk import your data:
```bash
curl -X POST http://localhost:44372/api/importer/import \
  -H "Content-Type: application/json" \
  -d @import-data.json
```

## How It Works

### Inventory Management
- When you import rooms/events with prices and availability, inventory entries are created
- Each date gets its own inventory entry with total quantity and price
- Booked quantity starts at 0 and increases as bookings are made

### Booking Flow
1. User selects dates and makes booking request
2. System checks availability for all dates in range
3. If available, booking is created and inventory is updated
4. Booked quantity increases for each date
5. If booking is cancelled, inventory is released

### Event Capacity
- Events have a single date and capacity (total seats)
- When tickets are booked, booked quantity increases
- System prevents overbooking

## Next Steps

1. **User Authentication**: Add login system for users to manage their bookings
2. **Admin Dashboard**: Create admin interface for managing bookings and inventory
3. **Real-time Updates**: Add signal notifications for inventory changes
4. **Reporting**: Add booking reports and revenue analytics

