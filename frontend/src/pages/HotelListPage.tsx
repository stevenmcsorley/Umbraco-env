import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { hotelsApi } from '../services/api/hotelsApi';
import type { Hotel } from '../shared-types/domain.types';
import { Hero } from '../components/Hero';

export const HotelListPage = () => {
  const [hotels, setHotels] = useState<Hotel[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    hotelsApi.getHotels()
      .then(setHotels)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="p-8 text-center">Loading...</div>;

  return (
    <div>
      <Hero title="Hotels" subtitle="Discover our amazing hotels" />
      <div className="container mx-auto px-4 py-8">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {hotels.map((hotel) => (
            <Link
              key={hotel.id}
              to={`/hotels/${hotel.id}`}
              className="border rounded-lg p-6 shadow-md hover:shadow-lg transition-shadow"
            >
              <h2 className="text-2xl font-bold mb-2">{hotel.name}</h2>
              {hotel.description && <p className="text-gray-600 mb-4">{hotel.description}</p>}
              {hotel.city && hotel.country && (
                <p className="text-sm text-gray-500">{hotel.city}, {hotel.country}</p>
              )}
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
};

