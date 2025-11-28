import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { hotelsApi } from '../services/api/hotelsApi';
import type { Hotel, Room, Offer } from '../shared-types/domain.types';
import { Hero } from '../components/Hero';
import { RoomCard } from '../components/RoomCard';
import { OfferCard } from '../components/OfferCard';

export const HotelDetailsPage = () => {
  const { id } = useParams<{ id: string }>();
  const [hotel, setHotel] = useState<Hotel | null>(null);
  const [rooms, setRooms] = useState<Room[]>([]);
  const [offers, setOffers] = useState<Offer[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;

    Promise.all([
      hotelsApi.getHotel(id),
      hotelsApi.getRooms(id),
      hotelsApi.getOffers(id)
    ])
      .then(([hotelData, roomsData, offersData]) => {
        setHotel(hotelData);
        setRooms(roomsData);
        setOffers(offersData);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="p-8 text-center">Loading...</div>;
  if (!hotel) return <div className="p-8 text-center">Hotel not found</div>;

  return (
    <div>
      <Hero title={hotel.name} subtitle={hotel.description} />
      <div className="container mx-auto px-4 py-8">
        <div className="mb-8">
          <h2 className="text-3xl font-bold mb-4">About</h2>
          <p className="text-gray-700 mb-4">{hotel.description}</p>
          {hotel.address && (
            <div className="text-gray-600">
              <p>{hotel.address}</p>
              {hotel.city && hotel.country && (
                <p>{hotel.city}, {hotel.country}</p>
              )}
              {hotel.phone && <p>Phone: {hotel.phone}</p>}
              {hotel.email && <p>Email: {hotel.email}</p>}
            </div>
          )}
        </div>

        {offers.length > 0 && (
          <div className="mb-8">
            <h2 className="text-3xl font-bold mb-4">Special Offers</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {offers.map((offer) => (
                <OfferCard key={offer.id} offer={offer} />
              ))}
            </div>
          </div>
        )}

        {rooms.length > 0 && (
          <div>
            <h2 className="text-3xl font-bold mb-4">Rooms</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {rooms.map((room) => (
                <RoomCard key={room.id} room={room} hotelId={id!} />
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

