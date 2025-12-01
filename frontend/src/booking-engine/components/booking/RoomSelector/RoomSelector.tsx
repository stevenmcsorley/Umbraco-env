import { useState, useEffect } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';

export interface Room {
  id: string;
  name: string;
  description?: string;
  priceFrom?: number;
  maxOccupancy?: number;
  bedType?: string;
  size?: string;
  image?: string;
}

export const RoomSelector = ({ hotelId }: { hotelId: string }) => {
  const { selectedProductId, setSelectedProductId } = useBookingStore();
  const [rooms, setRooms] = useState<Room[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!hotelId) {
      setRooms([]);
      setLoading(false);
      return;
    }

    const fetchRooms = async () => {
      try {
        setLoading(true);
        const response = await fetch(`/api/hotels/${hotelId}/rooms`);
        if (!response.ok) {
          throw new Error('Failed to fetch rooms');
        }
        const data = await response.json();
        setRooms(data || []);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load rooms');
      } finally {
        setLoading(false);
      }
    };

    fetchRooms();
  }, [hotelId]);

  if (!hotelId) {
    return null;
  }

  if (loading) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <div style={{ display: 'inline-block', width: '20px', height: '20px', border: '2px solid #e5e7eb', borderTopColor: '#111827', borderRadius: '50%', animation: 'spin 1s linear infinite' }}></div>
        <p style={{ marginTop: '12px', color: '#6b7280' }}>Loading rooms...</p>
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

  if (rooms.length === 0) {
    return (
      <div style={{ padding: '20px', textAlign: 'center', color: '#6b7280' }}>
        <p>No rooms available for this hotel</p>
      </div>
    );
  }

  return (
    <div style={{ marginBottom: '32px' }}>
      <h2 style={{ fontSize: '18px', fontWeight: '600', marginBottom: '16px', color: '#111827' }}>
        Select a Room
      </h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '16px' }}>
        {rooms.map((room) => (
          <div
            key={room.id}
            onClick={() => setSelectedProductId(room.id)}
            style={{
              border: selectedProductId === room.id ? '2px solid #111827' : '1px solid #e5e7eb',
              borderRadius: '8px',
              padding: '16px',
              cursor: 'pointer',
              transition: 'all 0.2s',
              backgroundColor: selectedProductId === room.id ? '#f9fafb' : 'white',
              boxShadow: selectedProductId === room.id ? '0 4px 6px rgba(0,0,0,0.1)' : 'none'
            }}
            onMouseEnter={(e) => {
              if (selectedProductId !== room.id) {
                e.currentTarget.style.borderColor = '#d1d5db';
                e.currentTarget.style.boxShadow = '0 2px 4px rgba(0,0,0,0.05)';
              }
            }}
            onMouseLeave={(e) => {
              if (selectedProductId !== room.id) {
                e.currentTarget.style.borderColor = '#e5e7eb';
                e.currentTarget.style.boxShadow = 'none';
              }
            }}
          >
            <h3 style={{ fontSize: '16px', fontWeight: '600', marginBottom: '8px', color: '#111827' }}>
              {room.name}
            </h3>
            {room.description && (
              <p style={{ fontSize: '14px', color: '#6b7280', marginBottom: '8px', lineHeight: '1.5' }}>
                {room.description.substring(0, 100)}{room.description.length > 100 ? '...' : ''}
              </p>
            )}
            <div style={{ display: 'flex', gap: '12px', marginTop: '8px', fontSize: '12px', color: '#6b7280' }}>
              {room.maxOccupancy && <span>Sleeps {room.maxOccupancy}</span>}
              {room.bedType && <span>• {room.bedType}</span>}
              {room.size && <span>• {room.size}</span>}
            </div>
            {room.priceFrom && (
              <p style={{ fontSize: '16px', fontWeight: '600', color: '#111827', marginTop: '12px' }}>
                From £{room.priceFrom.toFixed(2)} per night
              </p>
            )}
            {selectedProductId === room.id && (
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

