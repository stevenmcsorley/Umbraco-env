import { useState, useEffect } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';

export interface Hotel {
  id: string;
  name: string;
  location?: string;
  description?: string;
  image?: string;
  priceFrom?: number;
}

export const HotelSelector = () => {
  const { selectedHotelId, setSelectedHotelId } = useBookingStore();
  const [hotels, setHotels] = useState<Hotel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchHotels = async () => {
      try {
        setLoading(true);
        const response = await fetch('/api/hotels');
        if (!response.ok) {
          throw new Error('Failed to fetch hotels');
        }
        const data = await response.json();
        setHotels(data || []);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load hotels');
      } finally {
        setLoading(false);
      }
    };

    fetchHotels();
  }, []);

  if (loading) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <div style={{ display: 'inline-block', width: '20px', height: '20px', border: '2px solid #e5e7eb', borderTopColor: '#111827', borderRadius: '50%', animation: 'spin 1s linear infinite' }}></div>
        <p style={{ marginTop: '12px', color: '#6b7280' }}>Loading hotels...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: '20px', textAlign: 'center', color: '#dc2626' }}>
        <p>Error: {error}</p>
      </div>
    );
  }

  if (hotels.length === 0) {
    return (
      <div style={{ padding: '20px', textAlign: 'center', color: '#6b7280' }}>
        <p>No hotels available</p>
      </div>
    );
  }

  return (
    <div style={{ marginBottom: '32px' }}>
      <h2 style={{ fontSize: '18px', fontWeight: '600', marginBottom: '16px', color: '#111827' }}>
        Select a Hotel
      </h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '16px' }}>
        {hotels.map((hotel) => (
          <div
            key={hotel.id}
            onClick={() => setSelectedHotelId(hotel.id)}
            style={{
              border: selectedHotelId === hotel.id ? '2px solid #111827' : '1px solid #e5e7eb',
              borderRadius: '8px',
              padding: '16px',
              cursor: 'pointer',
              transition: 'all 0.2s',
              backgroundColor: selectedHotelId === hotel.id ? '#f9fafb' : 'white',
              boxShadow: selectedHotelId === hotel.id ? '0 4px 6px rgba(0,0,0,0.1)' : 'none'
            }}
            onMouseEnter={(e) => {
              if (selectedHotelId !== hotel.id) {
                e.currentTarget.style.borderColor = '#d1d5db';
                e.currentTarget.style.boxShadow = '0 2px 4px rgba(0,0,0,0.05)';
              }
            }}
            onMouseLeave={(e) => {
              if (selectedHotelId !== hotel.id) {
                e.currentTarget.style.borderColor = '#e5e7eb';
                e.currentTarget.style.boxShadow = 'none';
              }
            }}
          >
            <h3 style={{ fontSize: '16px', fontWeight: '600', marginBottom: '8px', color: '#111827' }}>
              {hotel.name}
            </h3>
            {hotel.location && (
              <p style={{ fontSize: '14px', color: '#6b7280', marginBottom: '8px' }}>
                {hotel.location}
              </p>
            )}
            {hotel.priceFrom && (
              <p style={{ fontSize: '14px', fontWeight: '500', color: '#111827', marginTop: '8px' }}>
                From £{hotel.priceFrom.toFixed(2)} per night
              </p>
            )}
            {selectedHotelId === hotel.id && (
              <div style={{ marginTop: '12px', padding: '8px', backgroundColor: '#111827', color: 'white', borderRadius: '4px', fontSize: '12px', fontWeight: '500', textAlign: 'center' }}>
                Selected
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};

