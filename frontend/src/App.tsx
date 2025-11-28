import { Routes, Route } from 'react-router-dom';
import { HotelListPage } from './pages/HotelListPage';
import { HotelDetailsPage } from './pages/HotelDetailsPage';
import { RoomPage } from './pages/RoomPage';
import { OffersPage } from './pages/OffersPage';

export const App = () => {
  return (
    <Routes>
      <Route path="/" element={<HotelListPage />} />
      <Route path="/hotels/:id" element={<HotelDetailsPage />} />
      <Route path="/hotels/:hotelId/rooms/:roomId" element={<RoomPage />} />
      <Route path="/hotels/:id/offers" element={<OffersPage />} />
    </Routes>
  );
};

