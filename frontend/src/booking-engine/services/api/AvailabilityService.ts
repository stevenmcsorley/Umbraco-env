import type { AvailabilityRequest, AvailabilityResponse } from '../../types/domain.types';
import { AvailabilityAdapter } from '../adapters/AvailabilityAdapter';

export class AvailabilityService {
  private static readonly BASE = '/engine/availability';

  static async getRange(request: AvailabilityRequest): Promise<AvailabilityResponse> {
    const params = new URLSearchParams({
      productId: request.productId,
      from: typeof request.from === 'string' ? request.from : request.from.toISOString(),
      to: typeof request.to === 'string' ? request.to : request.to.toISOString()
    });

    const response = await fetch(`${this.BASE}?${params}`);
    if (!response.ok) {
      throw new Error('Failed to fetch availability');
    }

    const data = await response.json();
    return AvailabilityAdapter.fromAPI(data);
  }
}

