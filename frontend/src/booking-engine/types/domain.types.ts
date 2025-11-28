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

