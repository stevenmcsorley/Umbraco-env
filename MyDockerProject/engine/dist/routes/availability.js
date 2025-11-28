"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.availabilityRouter = void 0;
const express_1 = require("express");
const AvailabilityService_1 = require("../services/AvailabilityService");
exports.availabilityRouter = (0, express_1.Router)();
exports.availabilityRouter.get('/', async (req, res) => {
    try {
        const { productId, from, to } = req.query;
        if (!productId || !from || !to) {
            return res.status(400).json({
                error: 'Missing required parameters: productId, from, to'
            });
        }
        const fromDate = new Date(from);
        const toDate = new Date(to);
        if (isNaN(fromDate.getTime()) || isNaN(toDate.getTime())) {
            return res.status(400).json({
                error: 'Invalid date format'
            });
        }
        const availability = await AvailabilityService_1.AvailabilityService.getAvailability(productId, fromDate, toDate);
        res.json(availability);
    }
    catch (error) {
        console.error('Availability error:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});
