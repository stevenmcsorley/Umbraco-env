import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { hotelsApi } from '../services/api/hotelsApi';
import type { Room } from '../shared-types/domain.types';
import { BookingApp } from '../booking-engine/app/BookingApp';

export const RoomPage = () => {
  const { hotelId, roomId } = useParams<{ hotelId: string; roomId: string }>();
  const [room, setRoom] = useState<Room | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!hotelId || !roomId) return;

    hotelsApi.getRooms(hotelId)
      .then((rooms) => {
        const foundRoom = rooms.find((r) => r.id === roomId);
        setRoom(foundRoom || null);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [hotelId, roomId]);

  if (loading) return <div className="p-8 text-center">Loading...</div>;
  if (!room) return <div className="p-8 text-center">Room not found</div>;

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-4xl font-bold mb-4">{room.name}</h1>
      {room.description && <p className="text-gray-700 mb-8">{room.description}</p>}
      
      <div className="mb-8">
        <div className="grid grid-cols-2 gap-4 mb-4">
          {room.maxOccupancy && (
            <div>
              <strong>Max Occupancy:</strong> {room.maxOccupancy}
            </div>
          )}
          {room.priceFrom && (
            <div>
              <strong>Price From:</strong> £{room.priceFrom.toFixed(2)}
            </div>
          )}
          {room.roomType && (
            <div>
              <strong>Room Type:</strong> {room.roomType}
            </div>
          )}
        </div>
      </div>

      <div className="border-t pt-8">
        <h2 className="text-2xl font-bold mb-4">Book This Room</h2>
        <BookingApp
          hotelId={hotelId}
          apiBaseUrl="/engine"
          defaultProductId={roomId}
        />
      </div>
    </div>
  );
};

