import { Router } from 'express';
import { AvailabilityService } from '../services/AvailabilityService';

export const availabilityRouter = Router();

availabilityRouter.get('/', async (req, res) => {
  try {
    const { productId, from, to } = req.query;

    if (!productId || !from || !to) {
      return res.status(400).json({
        error: 'Missing required parameters: productId, from, to'
      });
    }

    const fromDate = new Date(from as string);
    const toDate = new Date(to as string);

    if (isNaN(fromDate.getTime()) || isNaN(toDate.getTime())) {
      return res.status(400).json({
        error: 'Invalid date format'
      });
    }

    const availability = await AvailabilityService.getAvailability(
      productId as string,
      fromDate,
      toDate
    );

    res.json(availability);
  } catch (error) {
    console.error('Availability error:', error);
    res.status(500).json({ error: 'Internal server error' });
  }
});

