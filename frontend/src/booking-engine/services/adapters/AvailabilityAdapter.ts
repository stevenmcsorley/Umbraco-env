import type { AvailabilityResponse } from '../../types/domain.types';

export class AvailabilityAdapter {
  static fromAPI(data: any): AvailabilityResponse {
    return {
      productId: data.productId,
      from: new Date(data.from),
      to: new Date(data.to),
      days: data.days.map((day: any) => ({
        date: new Date(day.date),
        available: day.available,
        price: day.price,
        unitsAvailable: day.unitsAvailable || 0
      })),
      currency: data.currency || 'GBP'
    };
  }
}

