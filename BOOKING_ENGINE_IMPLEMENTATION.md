# Booking Engine Implementation Summary

## Overview

The booking engine has been enhanced to integrate with Umbraco CMS data and support a universal product model with add-ons functionality. The implementation follows the PRD requirements and maintains the existing React component style guide.

## Key Features Implemented

### 1. Universal Product Model
- **Product Types Supported**: `room`, `event`, `ticket`, `pass`, `timeslot`, `bookable-unit`
- Products can be associated with hotels and include flexible attributes
- Products support dynamic pricing from Umbraco content

### 2. Umbraco Integration
- **UmbracoAdapter**: New adapter that fetches real data from Umbraco HotelsController API
- Fetches room and event data with prices from Umbraco content
- Supports both GUID and slug-based product identification
- Falls back to LocalJsonAdapter for testing/development

### 3. Add-ons Support
- **Add-on Types**: `one-time`, `per-night`, `per-person`, `per-unit`
- Add-ons can be selected during booking
- Pricing calculations handle different add-on pricing models
- Add-ons displayed in booking summary and confirmation

### 4. Enhanced Booking Flow
- Calendar selector with availability visualization
- Availability panel showing base price + add-ons breakdown
- Booking form with guest details
- Confirmation screen with total pricing and add-ons summary

## Architecture

### Backend (Node.js/Express)
- **Location**: `MyDockerProject/engine/`
- **Adapters**:
  - `UmbracoAdapter`: Fetches data from Umbraco API
  - `LocalJsonAdapter`: Mock data for testing
- **Services**:
  - `AvailabilityService`: Manages availability queries
  - `BookingService`: Handles booking creation with add-ons pricing

### Frontend (React/TypeScript)
- **Location**: `frontend/src/booking-engine/`
- **Components**:
  - `BookingApp`: Main booking application container
  - `CalendarSelector`: Date selection with availability
  - `AvailabilityPanel`: Shows pricing breakdown
  - `AddOnsSelector`: Add-on selection interface
  - `BookingForm`: Guest information form
  - `ConfirmationScreen`: Booking confirmation display

### State Management
- Uses Zustand for booking state
- Tracks: selected product, dates, availability, add-ons, confirmation

## API Integration

### Umbraco API Endpoints Used
- `GET /api/hotels` - List all hotels
- `GET /api/hotels/{identifier}/rooms` - Get rooms for a hotel
- `GET /api/hotels/{identifier}/events` - Get events for a hotel
- `GET /api/hotels/{identifier}/availability` - Get availability data

### Booking Engine Endpoints
- `GET /engine/availability?productId={id}&from={date}&to={date}` - Get availability
- `POST /engine/book` - Create booking with add-ons

## Configuration

### Environment Variables (Booking Engine Service)
- `UMBRACO_API_BASE`: Base URL for Umbraco API (defaults to `http://mydockerproject:8080/api` in Docker)
- `USE_UMBRACO_ADAPTER`: Enable/disable Umbraco adapter (default: true)
- `PORT`: Booking engine port (default: 3001)

### Docker Configuration
The booking engine is configured in `MyDockerProject/docker-compose.yml`:
- **booking_engine** service: Node.js/Express service running on port 3001
- Connects to Umbraco service via Docker network (`umbnet`)
- Uses service name `mydockerproject:8080` for API calls
- Environment variables configured for Umbraco integration
- **frontend** service removed (React is built and served from Umbraco wwwroot)

### Building the React Booking Engine
The React booking engine must be built before deployment:
1. Run `MyDockerProject/build-booking-engine.sh` OR
2. Manually: `cd frontend && npm run build && cp dist/booking-engine.js MyDockerProject/MyDockerProject/wwwroot/scripts/`

See `BUILD_BOOKING_ENGINE.md` for detailed instructions.

## Usage

### Basic Booking Flow
1. User selects a product (room/event) via `defaultProductId` prop
2. User selects dates using calendar selector
3. System fetches availability from Umbraco
4. User can select add-ons (if available)
5. User fills in guest details
6. Booking is created with total price including add-ons
7. Confirmation screen displays booking details

### Component Usage
```tsx
<BookingApp
  hotelId="hotel-guid"
  defaultProductId="room-guid"
  apiBaseUrl="/engine"
  tokens={customTokens}
/>
```

## Pricing Logic

### Base Price
- Calculated from availability days (sum of daily prices)
- Fetched from Umbraco room/event content

### Add-on Pricing
- **one-time**: Price × quantity
- **per-night**: Price × quantity × nights
- **per-person**: Price × quantity × guests
- **per-unit**: Price × quantity

### Total Price
- Base price + sum of all add-on prices

## Design Tokens & Theming

The booking engine supports theming via design tokens:
- Colors (brand, text, background)
- Spacing scale
- Typography
- Border radius
- Shadows

Tokens can be passed via `tokens` prop to `BookingApp`.

## Testing

Test IDs are available for all interactive components:
- Calendar days: `booking-calendar-day-{date}`
- Add-ons: `booking-addon-{action}-{id}`
- Form fields: `booking-{field}-input`
- Buttons: `booking-{action}-button`

## Future Enhancements

1. **Add-ons Content Type**: Create Umbraco content type for managing add-ons
2. **Dynamic Pricing**: Implement seasonal/dynamic pricing rules
3. **Payment Integration**: Add payment provider hooks
4. **Booking Persistence**: Store bookings in Umbraco/database
5. **Availability Rules**: Implement complex availability logic
6. **Multi-product Booking**: Support booking multiple products in one transaction

## Notes

- Currently, add-ons are placeholder (empty array) until Umbraco add-ons content type is created
- Availability is based on room/event existence; actual booking conflicts not yet checked
- Prices are fetched from Umbraco `priceFrom` property; dynamic pricing can be added later
- The engine is designed to be adaptable and can work with different product types beyond hotels

## Files Modified/Created

### Backend (Docker: MyDockerProject/engine/)
- `MyDockerProject/engine/src/adapters/UmbracoAdapter.ts` (new) - Fetches data from Umbraco API
- `MyDockerProject/engine/src/services/AvailabilityService.ts` (updated) - Uses UmbracoAdapter
- `MyDockerProject/engine/src/services/BookingService.ts` (updated) - Handles add-ons pricing
- `MyDockerProject/engine/src/types/domain.types.ts` (updated) - Added product types and add-ons
- `MyDockerProject/docker-compose.yml` (updated) - Removed frontend service, configured engine

### Frontend React (Root: frontend/)
**Note**: The React booking engine is built separately and copied to `wwwroot/scripts/`
- `frontend/src/booking-engine/types/domain.types.ts` (updated)
- `frontend/src/booking-engine/app/state/bookingStore.ts` (updated) - Added add-ons state
- `frontend/src/booking-engine/app/BookingApp.tsx` (updated) - Added AddOnsSelector
- `frontend/src/booking-engine/components/booking/AddOnsSelector/` (new)
- `frontend/src/booking-engine/components/booking/AvailabilityPanel/AvailabilityPanel.tsx` (updated) - Shows add-ons pricing
- `frontend/src/booking-engine/components/booking/BookingForm/BookingForm.tsx` (updated) - Includes add-ons
- `frontend/src/booking-engine/components/booking/ConfirmationScreen/ConfirmationScreen.tsx` (updated) - Shows add-ons
- `frontend/src/booking-engine/services/adapters/BookingAdapter.ts` (updated) - Handles add-ons
- `frontend/src/booking-engine/utils/priceUtils.ts` (updated) - Add-on pricing calculations
- `frontend/src/booking-engine/constants/testids.ts` (updated) - Add-on test IDs

### Umbraco Views (Docker: MyDockerProject/MyDockerProject/Views/)
- `MyDockerProject/MyDockerProject/Views/Partials/BookingEngine.cshtml` (updated) - Uses `/scripts/booking-engine.js`
- `MyDockerProject/MyDockerProject/Views/room.cshtml` (updated) - Uses `/scripts/booking-engine.js`

### Build Scripts
- `MyDockerProject/build-booking-engine.sh` (new) - Builds React and copies to wwwroot
- `BUILD_BOOKING_ENGINE.md` (new) - Build instructions

