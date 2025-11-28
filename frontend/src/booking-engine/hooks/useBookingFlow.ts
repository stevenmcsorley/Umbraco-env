import { useState } from 'react';
import { BookingService } from '../services/api/BookingService';
import type { BookingRequest, BookingResponse } from '../types/domain.types';

export const useBookingFlow = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [booking, setBooking] = useState<BookingResponse | null>(null);

  const submitBooking = async (request: BookingRequest) => {
    setLoading(true);
    setError(null);

    try {
      const result = await BookingService.create(request);
      setBooking(result);
      return result;
    } catch (err: any) {
      setError(err.message);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const reset = () => {
    setBooking(null);
    setError(null);
    setLoading(false);
  };

  return { submitBooking, loading, error, booking, reset };
};

