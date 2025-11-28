"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.AvailabilityService = void 0;
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
}
exports.AvailabilityService = AvailabilityService;
AvailabilityService.adapter = new LocalJsonAdapter_1.LocalJsonAdapter();
