import type { BookingRequest, BookingResponse } from '../../types/domain.types';

export class BookingAdapter {
  static toAPI(request: BookingRequest): any {
    return {
      productId: request.productId,
      from: typeof request.from === 'string' ? request.from : request.from.toISOString(),
      to: typeof request.to === 'string' ? request.to : request.to.toISOString(),
      guestDetails: request.guestDetails,
      userId: request.userId,
      quantity: request.quantity || 1,
      addOns: request.addOns || [],
      events: request.events || []
    };
  }

  static fromAPI(data: any): BookingResponse {
    return {
      bookingId: data.bookingId || data.bookingReference,
      productId: data.productId,
      productName: data.productName,
      hotelName: data.hotelName,
      hotelLocation: data.hotelLocation,
      from: data.checkIn ? new Date(data.checkIn) : new Date(data.from),
      to: data.checkOut ? new Date(data.checkOut) : (data.to ? new Date(data.to) : new Date(data.from)),
      guestDetails: data.guestDetails || {
        firstName: data.guestFirstName || '',
        lastName: data.guestLastName || '',
        email: data.guestEmail || '',
        phone: data.guestPhone
      },
      status: data.status,
      createdAt: data.createdAt ? new Date(data.createdAt) : new Date(),
      totalPrice: data.totalPrice,
      currency: data.currency || 'GBP',
      addOns: data.addOns || []
    };
  }
}

