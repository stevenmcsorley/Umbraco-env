import { UmbracoAdapter } from '../adapters/UmbracoAdapter';
import { LocalJsonAdapter } from '../adapters/LocalJsonAdapter';
import type { AvailabilityRequest, AvailabilityResponse } from '../types/domain.types';

export class AvailabilityService {
  // Use UmbracoAdapter by default, fallback to LocalJsonAdapter for testing
  private static adapter = process.env.USE_UMBRACO_ADAPTER !== 'false' 
    ? new UmbracoAdapter() 
    : new LocalJsonAdapter();

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

  /**
   * Gets product information
   */
  static async getProduct(productId: string) {
    if (this.adapter instanceof UmbracoAdapter) {
      return await this.adapter.getProduct(productId);
    }
    return null;
  }

  /**
   * Gets add-ons for a hotel/product
   */
  static async getAddOns(hotelId: string) {
    if (this.adapter instanceof UmbracoAdapter) {
      return await this.adapter.getAddOns(hotelId);
    }
    return [];
  }
}

