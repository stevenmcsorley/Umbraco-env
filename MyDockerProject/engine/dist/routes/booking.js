"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.bookingRouter = void 0;
const express_1 = require("express");
const BookingService_1 = require("../services/BookingService");
exports.bookingRouter = (0, express_1.Router)();
exports.bookingRouter.post('/', async (req, res) => {
    try {
        const bookingRequest = req.body;
        if (!bookingRequest.productId || !bookingRequest.from || !bookingRequest.to) {
            return res.status(400).json({
                error: 'Missing required fields: productId, from, to'
            });
        }
        const booking = await BookingService_1.BookingService.createBooking(bookingRequest);
        res.status(201).json(booking);
    }
    catch (error) {
        console.error('Booking error:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});
