import type { AvailabilityRequest, AvailabilityResponse, BookingRequest, BookingResponse } from '../../shared-types/domain.types';

const ENGINE_BASE = '/engine';

export const bookingEngineApi = {
  async getAvailability(request: AvailabilityRequest): Promise<AvailabilityResponse> {
    const params = new URLSearchParams({
      productId: request.productId,
      from: typeof request.from === 'string' ? request.from : request.from.toISOString(),
      to: typeof request.to === 'string' ? request.to : request.to.toISOString()
    });

    const response = await fetch(`${ENGINE_BASE}/availability?${params}`);
    if (!response.ok) throw new Error('Failed to fetch availability');
    return response.json();
  },

  async createBooking(request: BookingRequest): Promise<BookingResponse> {
    const response = await fetch(`${ENGINE_BASE}/book`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ...request,
        from: typeof request.from === 'string' ? request.from : request.from.toISOString(),
        to: typeof request.to === 'string' ? request.to : request.to.toISOString()
      })
    });
    if (!response.ok) throw new Error('Failed to create booking');
    return response.json();
  }
};

