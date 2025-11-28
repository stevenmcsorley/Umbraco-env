"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const express_1 = __importDefault(require("express"));
const cors_1 = __importDefault(require("cors"));
const availability_1 = require("./routes/availability");
const booking_1 = require("./routes/booking");
const app = (0, express_1.default)();
const PORT = process.env.PORT || 3001;
app.use((0, cors_1.default)());
app.use(express_1.default.json());
app.use('/availability', availability_1.availabilityRouter);
app.use('/book', booking_1.bookingRouter);
app.get('/health', (req, res) => {
    res.json({ status: 'ok', service: 'booking-engine' });
});
app.listen(PORT, () => {
    console.log(`Booking Engine backend running on port ${PORT}`);
});
