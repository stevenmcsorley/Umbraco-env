export type ProductType = 'room' | 'event' | 'ticket' | 'pass' | 'timeslot' | 'bookable-unit';

export interface Product {
  id: string;
  type: ProductType;
  title: string;
  description?: string;
  images?: string[];
  priceFrom?: number;
  attributes?: Record<string, string | number>;
  hotelId?: string;
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
  addOns?: Array<{
    addOnId: string;
    quantity: number;
  }>;
  events?: Array<{
    eventId: string;
  }>;
}

export interface BookingResponse {
  bookingId: string;
  productId: string;
  from: Date | string;
  to: Date | string;
  guestDetails: GuestDetails;
  status: string;
  createdAt: Date | string;
  totalPrice?: number;
  currency?: string;
  addOns?: Array<{
    addOnId: string;
    name: string;
    quantity: number;
    price: number;
  }>;
}

