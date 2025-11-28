import { Router } from 'express';
import { BookingService } from '../services/BookingService';
import type { BookingRequest } from '../types/domain.types';

export const bookingRouter = Router();

bookingRouter.post('/', async (req, res) => {
  try {
    const bookingRequest = req.body as BookingRequest;

    if (!bookingRequest.productId || !bookingRequest.from || !bookingRequest.to) {
      return res.status(400).json({
        error: 'Missing required fields: productId, from, to'
      });
    }

    const booking = await BookingService.createBooking(bookingRequest);

    res.status(201).json(booking);
  } catch (error) {
    console.error('Booking error:', error);
    res.status(500).json({ error: 'Internal server error' });
  }
});

