import type { AvailabilityRequest, AvailabilityResponse, CalendarDay } from '../types/domain.types';

export class LocalJsonAdapter {
  async getAvailability(request: AvailabilityRequest): Promise<AvailabilityResponse> {
    // Mock availability data - deterministic for testing
    const days: CalendarDay[] = [];
    const fromDate = new Date(request.from);
    const toDate = new Date(request.to);

    let currentDate = new Date(fromDate);
    while (currentDate <= toDate) {
      // Mock: 80% availability, random prices between 100-300
      const isAvailable = Math.random() > 0.2;
      const price = isAvailable ? Math.floor(Math.random() * 200) + 100 : null;

      days.push({
        date: new Date(currentDate),
        available: isAvailable,
        price: price ?? undefined,
        unitsAvailable: isAvailable ? Math.floor(Math.random() * 5) + 1 : 0
      });

      currentDate.setDate(currentDate.getDate() + 1);
    }

    return {
      productId: request.productId,
      from: request.from,
      to: request.to,
      days,
      currency: 'GBP'
    };
  }
}

