import { LocalJsonAdapter } from '../adapters/LocalJsonAdapter';
import type { AvailabilityRequest, AvailabilityResponse } from '../types/domain.types';

export class AvailabilityService {
  private static adapter = new LocalJsonAdapter();

  static async getAvailability(
    productId: string,
    from: Date,
    to: Date
  ): Promise<AvailabilityResponse> {
    const request: AvailabilityRequest = {
      productId,
      from,
      to
    };

    return await this.adapter.getAvailability(request);
  }
}

