"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.BookingService = void 0;
const AvailabilityService_1 = require("./AvailabilityService");
class BookingService {
    static async createBooking(request) {
        // Mock booking creation - in real implementation, this would persist to database
        const bookingReference = `BK-${Date.now()}-${Math.random().toString(36).substr(2, 9).toUpperCase()}`;
        // Calculate total price including add-ons
        const product = await AvailabilityService_1.AvailabilityService.getProduct(request.productId);
        const fromDate = new Date(request.from);
        const toDate = new Date(request.to);
        // Calculate number of nights/days
        const nights = Math.ceil((toDate.getTime() - fromDate.getTime()) / (1000 * 60 * 60 * 24));
        // Get base price from availability
        const availability = await AvailabilityService_1.AvailabilityService.getAvailability(request.productId, fromDate, toDate);
        const basePrice = availability.days.reduce((sum, day) => sum + (day.price || 0), 0);
        // Calculate add-on prices
        let addOnsTotal = 0;
        const addOnsDetails = [];
        if (request.addOns && request.addOns.length > 0) {
            // Get available add-ons (would normally fetch from Umbraco)
            // For now, calculate based on request
            for (const addOnRequest of request.addOns) {
                // In real implementation, fetch add-on details from Umbraco
                // For now, use placeholder pricing
                const addOnPrice = 10; // Placeholder - would come from Umbraco
                const addOnTotal = addOnPrice * addOnRequest.quantity *
                    (nights > 0 ? nights : 1); // Apply per-night logic if applicable
                addOnsTotal += addOnTotal;
                addOnsDetails.push({
                    addOnId: addOnRequest.addOnId,
                    name: `Add-on ${addOnRequest.addOnId}`, // Would come from Umbraco
                    quantity: addOnRequest.quantity,
                    price: addOnTotal
                });
            }
        }
        const totalPrice = basePrice + addOnsTotal;
        const response = {
            bookingId: bookingReference,
            productId: request.productId,
            from: request.from,
            to: request.to,
            guestDetails: request.guestDetails,
            status: 'confirmed',
            createdAt: new Date(),
            totalPrice,
            currency: availability.currency || 'GBP',
            addOns: addOnsDetails.length > 0 ? addOnsDetails : undefined
        };
        return response;
    }
}
exports.BookingService = BookingService;
