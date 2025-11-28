import type { BookingRequest, BookingResponse } from '../../types/domain.types';
import { BookingAdapter } from '../adapters/BookingAdapter';

export class BookingService {
  private static readonly BASE = '/engine/book';

  static async create(request: BookingRequest): Promise<BookingResponse> {
    const apiPayload = BookingAdapter.toAPI(request);

    const response = await fetch(this.BASE, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(apiPayload)
    });

    if (!response.ok) {
      throw new Error('Failed to create booking');
    }

    const data = await response.json();
    return BookingAdapter.fromAPI(data);
  }
}

