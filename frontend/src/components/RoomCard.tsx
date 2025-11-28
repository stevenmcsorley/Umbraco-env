import { Link } from 'react-router-dom';
import type { Room } from '../shared-types/domain.types';

export interface RoomCardProps {
  room: Room;
  hotelId: string;
  className?: string;
}

export const RoomCard = ({ room, hotelId, className = '' }: RoomCardProps) => {
  return (
    <div className={`border rounded-lg p-4 shadow-md ${className}`}>
      <h3 className="text-xl font-semibold mb-2">{room.name}</h3>
      {room.description && <p className="text-gray-600 mb-4">{room.description}</p>}
      <div className="flex justify-between items-center mb-4">
        {room.maxOccupancy && (
          <span className="text-sm text-gray-500">Max occupancy: {room.maxOccupancy}</span>
        )}
        {room.priceFrom && (
          <span className="text-lg font-bold">From £{room.priceFrom.toFixed(2)}</span>
        )}
      </div>
      <Link
        to={`/hotels/${hotelId}/rooms/${room.id}`}
        className="block text-center bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700"
      >
        View Details
      </Link>
    </div>
  );
};

