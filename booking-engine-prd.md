📘 Product Requirements Document — React Booking Engine

Version: 1.0
Status: Authoritative
Scope: React Booking Engine ONLY (isolated subsystem)
Dependencies: Umbraco CMS APIs & Booking Engine API adapters
Style Standard: Mini Sentry UI Development Style Guide (as provided)

1. Purpose

The Booking Engine is a standalone, embeddable React application that:

Displays availability for bookable products (rooms, tickets, events, passes, time slots)

Allows users to select a unit (room/ticket/etc)

Allows users to select dates/time slots

Allows users to configure add-ons (if provided)

Provides a simple checkout/confirmation flow

Can be themed using design tokens

Can embed into any site (React component OR iframe OR script embed)

No extra features beyond what is written here should be implemented.

2. Architecture Requirements

(Strictly aligned with the Mini-Sentry UI Style Guide)

2.1 Project Structure

All Booking Engine source code must follow:

booking-engine/
└── src/
    ├── components/
    │   ├── ui/                 # Primitive UI components (Button, Input, Card, etc.)
    │   ├── booking/            # Booking-specific components
    │   └── layout/             # Layout containers
    │
    ├── services/
    │   ├── api/                # Availability + booking service
    │   └── adapters/           # Adapter layer → API → domain models
    │
    ├── hooks/                  # useAvailability, useBookingFlow, etc.
    ├── utils/                  # Pure functions (calendar math, price calc, etc.)
    ├── types/
    │   ├── api.types.ts
    │   ├── domain.types.ts
    │   ├── ui.types.ts
    │   └── index.ts
    │
    ├── constants/
    │   ├── testids.ts          # Centralised test IDs
    │   ├── tokens.ts           # Theming tokens (colors, spacing, radii, etc.)
    │   └── routes.ts
    │
    └── app/
        ├── BookingApp.tsx      # Main entrypoint for embedding
        ├── router.tsx          # If routing is needed
        └── state/              # Booking state machine (Zustand or internal)


No default exports.
All components: arrow functions, named exports.
All types in separate .types.ts files.
Props destructured with defaults inside function parameters.

3. Functional Requirements
3.1 Supported Product Types

The engine must support a generic, unified product type:

Room

Ticket

Event

Time slot

Pass

Bookable unit

A product must minimally expose:

id: string
title: string
description?: string
images?: string[]
priceFrom?: number
attributes?: Record<string, string | number>


Expand only what the PRD requires — no extras.

3.2 Availability Flow
Required API service calls (via services/api/):

GET /availability?productId&from&to

POST /book

The Booking Engine never directly fetches; it always uses service classes.

Required features:

Calendar selection (single range OR single date depending on product type)

Price display (priceFrom or returned from availability response)

Availability indicator per date/time slot

User selection of:

Product

Dates

Quantity (if ticket-based)

Add-ons (optional if provided by API)

3.3 Booking Flow

Required steps:

Product → date selection

Availability check (auto-run on date change)

User details (basic)

Confirm booking

Show booking confirmation

Checkout is simple (no payment integration).
Only POST data the Booking Engine API defines.

4. UI Requirements (strict)
4.1 Components to Build

Each must follow your style guide:

Primitive UI Components (src/components/ui/):

Button

Input

Card

Modal (if needed)

Badge

Spinner/Loader

Select

Calendar Day

Calendar Grid

Collapse / Accordion

Booking Components (src/components/booking/):

ProductCard

ProductDetails

CalendarSelector

AvailabilityPanel

BookingForm

ConfirmationScreen

Layout Components

PageSection

PageContainer

StickyFooter

All components use:

Arrow functions

Named exports

Type-only imports

.types.ts files

Composition patterns (no boolean toggles for rendering child blocks)

test IDs from /constants/testids.ts

5. Theming Requirements

The Booking Engine must be themeable using:

/constants/tokens.ts

Defines:

colors: {
  brandPrimary: string
  brandAccent: string
  text: string
  background: string
}

spacing: { xs, sm, md, lg, xl }

radius: { sm, md, lg }

shadows: { card, modal }

typography: {
  fontFamily: string
  sizes: { xs, sm, md, lg, xl, xxl }
}


No runtime theming engine needed — token object must be passed as a single prop into <BookingApp />.

6. State Management

Must use one of:

Internal reducer

Zustand

Jotai

Never use prop drilling.

State slices:

selectedProductId
selectedDateRange
availabilityResults
bookingPayload
confirmation
ui: {
  loading,
  error
}

7. Analytics Requirements

Must follow PRD:

Multiple GA tags per hotel

Implement an analytics manager class with no real GA integration (stub only)

Provide:

trackEvent('booking:start', {...})
trackEvent('booking:availability_checked', {...})
trackEvent('booking:confirmed', {...})


Attach test IDs to all interactive elements:

Format:

data-testid="booking-{component}-{element}-{optional-id}"


Example:

data-testid="booking-calendar-day-2025-01-12"
data-testid="booking-product-card-ROOM123"
data-testid="booking-submit-button"

8. Embedding Requirements

The React Booking Engine must be embeddable in three ways:

1. React Component
<BookingApp
  hotelId="123"
  tokens={customTokens}
  apiBaseUrl="..."
/>

2. Iframe

Provide a minimal iframe wrapper (no styling).

3. Script Embed

Provide a small, optional script snippet example in docs.

9. Data Models (TypeScript)
Product
AvailabilityRequest
AvailabilityResponse
BookingRequest
BookingResponse
CalendarDay

All types placed under:

src/types/domain.types.ts
src/types/api.types.ts
src/types/ui.types.ts

10. API Service Layer

src/services/api/AvailabilityService.ts
src/services/api/BookingService.ts

Pattern (based on your style guide):

export class AvailabilityService {
  private static readonly BASE = `${API_BASE_URL}/availability`;

  static async getRange(request: AvailabilityRequest): Promise<AvailabilityResponse> {
    const res = await fetch(/* ... */)
    // Adapt using adapter
  }
}

11. Adapter Requirements

Adapters must transform:

API → domain

domain → API

All adapters go into:

src/services/adapters/


Naming examples:

AvailabilityAdapter.fromAPI(...)
BookingAdapter.toAPI(...)

12. No Features Beyond This Document

This PRD is strict.

No payments

No coupons

No taxes

No upsells unless provided in the data

No multi-room bookings

No wishlists

No account system

No extra UX flows

This is the complete, bounded specification.

13. Acceptance Criteria

The React Booking Engine is accepted when:

✓ All features listed above exist
✓ Component architecture matches the style guide
✓ Every interactive element has a proper test ID
✓ No default exports
✓ No API calls inside components (must use services + hooks)
✓ Theming tokens integrated
✓ Booking flow works end-to-end using mock API
✓ Code is clean, typed, and aligned to the style guide

No tests, no storybook, no CI required unless requested later.

14. Optional Future Extensions (Not Included)

Full importer integration

Multi-room bookings

Real payment provider integration

Real analytics integration

Multi-language

Advanced price matrix

Add-ons marketplace