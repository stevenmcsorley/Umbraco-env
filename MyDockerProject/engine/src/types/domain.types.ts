export type ProductType = 'room' | 'event' | 'ticket' | 'pass' | 'timeslot' | 'bookable-unit';

export interface Product {
  id: string;
  type: ProductType;
  title: string;
  description?: string;
  images?: string[];
  priceFrom?: number;
  attributes?: Record<string, string | number>;
  hotelId?: string; // For hotel-specific products
}

export interface AddOn {
  id: string;
  name: string;
  description?: string;
  price: number;
  type: 'one-time' | 'per-night' | 'per-person' | 'per-unit';
  available?: boolean;
  image?: string;
}

export interface AvailabilityRequest {
  productId: string;
  from: Date;
  to: Date;
}

export interface CalendarDay {
  date: Date;
  available: boolean;
  price?: number;
  unitsAvailable: number;
}

export interface AvailabilityResponse {
  productId: string;
  from: Date;
  to: Date;
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
  from: Date;
  to: Date;
  guestDetails: GuestDetails;
  quantity?: number;
  addOns?: Array<{
    addOnId: string;
    quantity: number;
  }>;
}

export interface BookingResponse {
  bookingId: string;
  productId: string;
  from: Date;
  to: Date;
  guestDetails: GuestDetails;
  status: string;
  createdAt: Date;
  totalPrice?: number;
  currency?: string;
  addOns?: Array<{
    addOnId: string;
    name: string;
    quantity: number;
    price: number;
  }>;
}

