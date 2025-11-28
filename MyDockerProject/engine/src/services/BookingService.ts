import type { BookingRequest, BookingResponse } from '../types/domain.types';

export class BookingService {
  static async createBooking(request: BookingRequest): Promise<BookingResponse> {
    // Mock booking creation - in real implementation, this would persist to database
    const bookingReference = `BK-${Date.now()}-${Math.random().toString(36).substr(2, 9).toUpperCase()}`;

    const response: BookingResponse = {
      bookingId: bookingReference,
      productId: request.productId,
      from: request.from,
      to: request.to,
      guestDetails: request.guestDetails,
      status: 'confirmed',
      createdAt: new Date()
    };

    return response;
  }
}

