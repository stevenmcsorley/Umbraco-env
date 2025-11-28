// Shared domain types used across CMS, Booking Engine, and Frontend

export interface Product {
  id: string;
  title: string;
  description?: string;
  images?: string[];
  priceFrom?: number;
  attributes?: Record<string, string | number>;
}

export interface AvailabilityRequest {
  productId: string;
  from: Date | string;
  to: Date | string;
}

export interface CalendarDay {
  date: Date | string;
  available: boolean;
  price?: number;
  unitsAvailable: number;
}

export interface AvailabilityResponse {
  productId: string;
  from: Date | string;
  to: Date | string;
  days: CalendarDay[];
  currency: string;
}

export interface GuestDetails {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
}

export interface BookingRequest {
  productId: string;
  from: Date | string;
  to: Date | string;
  guestDetails: GuestDetails;
  quantity?: number;
}

export interface BookingResponse {
  bookingId: string;
  productId: string;
  from: Date | string;
  to: Date | string;
  guestDetails: GuestDetails;
  status: string;
  createdAt: Date | string;
}

// CMS API types
export interface Hotel {
  id: string;
  name: string;
  description?: string;
  address?: string;
  city?: string;
  country?: string;
  phone?: string;
  email?: string;
  url?: string;
}

export interface Room {
  id: string;
  name: string;
  description?: string;
  maxOccupancy?: number;
  priceFrom?: number;
  roomType?: string;
  url?: string;
}

export interface Offer {
  id: string;
  name: string;
  description?: string;
  discount?: number;
  validFrom?: string;
  validTo?: string;
  url?: string;
}

