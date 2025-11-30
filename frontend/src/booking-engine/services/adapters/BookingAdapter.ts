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
      bookingId: data.bookingId,
      productId: data.productId,
      from: new Date(data.from),
      to: new Date(data.to),
      guestDetails: data.guestDetails,
      status: data.status,
      createdAt: new Date(data.createdAt),
      totalPrice: data.totalPrice,
      currency: data.currency || 'GBP',
      addOns: data.addOns || []
    };
  }
}

