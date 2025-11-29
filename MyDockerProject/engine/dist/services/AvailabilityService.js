"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.AvailabilityService = void 0;
const UmbracoAdapter_1 = require("../adapters/UmbracoAdapter");
const LocalJsonAdapter_1 = require("../adapters/LocalJsonAdapter");
class AvailabilityService {
    static async getAvailability(productId, from, to) {
        const request = {
            productId,
            from,
            to
        };
        return await this.adapter.getAvailability(request);
    }
    /**
     * Gets product information
     */
    static async getProduct(productId) {
        if (this.adapter instanceof UmbracoAdapter_1.UmbracoAdapter) {
            return await this.adapter.getProduct(productId);
        }
        return null;
    }
    /**
     * Gets add-ons for a hotel/product
     */
    static async getAddOns(hotelId) {
        if (this.adapter instanceof UmbracoAdapter_1.UmbracoAdapter) {
            return await this.adapter.getAddOns(hotelId);
        }
        return [];
    }
}
exports.AvailabilityService = AvailabilityService;
// Use UmbracoAdapter by default, fallback to LocalJsonAdapter for testing
AvailabilityService.adapter = process.env.USE_UMBRACO_ADAPTER !== 'false'
    ? new UmbracoAdapter_1.UmbracoAdapter()
    : new LocalJsonAdapter_1.LocalJsonAdapter();
