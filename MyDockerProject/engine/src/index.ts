import express from 'express';
import cors from 'cors';
import { availabilityRouter } from './routes/availability';
import { bookingRouter } from './routes/booking';

const app = express();
const PORT = process.env.PORT || 3001;

app.use(cors());
app.use(express.json());

app.use('/availability', availabilityRouter);
app.use('/book', bookingRouter);

app.get('/health', (req, res) => {
  res.json({ status: 'ok', service: 'booking-engine' });
});

app.listen(PORT, () => {
  console.log(`Booking Engine backend running on port ${PORT}`);
});

