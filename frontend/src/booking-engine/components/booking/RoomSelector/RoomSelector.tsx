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
  heroImage?: string;
  image?: string; // Keep for backward compatibility
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
      <div style={{ 
        padding: '60px 20px', 
        textAlign: 'center',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: '16px'
      }}>
        <div style={{ 
          display: 'inline-block', 
          width: '40px', 
          height: '40px', 
          border: '3px solid #f3f4f6', 
          borderTopColor: '#111827', 
          borderRadius: '50%', 
          animation: 'spin 0.8s linear infinite' 
        }}></div>
        <p style={{ 
          margin: 0, 
          color: '#6b7280',
          fontSize: '16px',
          fontWeight: '400'
        }}>Loading rooms...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ 
        padding: '40px 20px', 
        textAlign: 'center', 
        backgroundColor: '#fef2f2',
        border: '1px solid #fecaca',
        borderRadius: '8px',
        color: '#dc2626' 
      }}>
        <p style={{ margin: 0, fontSize: '16px', fontWeight: '500' }}>Error: {error}</p>
      </div>
    );
  }

  if (rooms.length === 0) {
    return (
      <div style={{ 
        padding: '40px 20px', 
        textAlign: 'center', 
        color: '#6b7280' 
      }}>
        <p style={{ margin: 0, fontSize: '16px' }}>No rooms available for this hotel</p>
      </div>
    );
  }

  return (
    <div style={{ marginBottom: '48px' }}>
      <div style={{ marginBottom: '32px' }}>
        <h2 style={{ 
          fontSize: '28px', 
          fontWeight: '600', 
          marginBottom: '8px', 
          color: '#111827',
          fontFamily: "'Playfair Display', serif",
          letterSpacing: '-0.02em'
        }}>
          Select a Room
        </h2>
        <p style={{ 
          fontSize: '16px', 
          color: '#6b7280',
          margin: 0,
          fontWeight: '300'
        }}>
          Choose the perfect accommodation for your stay
        </p>
      </div>
      <div style={{ 
        display: 'grid', 
        gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', 
        gap: '24px' 
      }}>
        {rooms.map((room) => {
          const isSelected = selectedProductId === room.id;
          return (
            <div
              key={room.id}
              onClick={() => setSelectedProductId(room.id)}
              style={{
                border: isSelected ? '2px solid #111827' : '1px solid #e5e7eb',
                borderRadius: '12px',
                overflow: 'hidden',
                cursor: 'pointer',
                transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
                backgroundColor: 'white',
                boxShadow: isSelected 
                  ? '0 10px 25px rgba(0,0,0,0.15), 0 4px 10px rgba(0,0,0,0.1)' 
                  : '0 1px 3px rgba(0,0,0,0.1)',
                transform: isSelected ? 'translateY(-2px)' : 'translateY(0)',
                position: 'relative'
              }}
              onMouseEnter={(e) => {
                if (!isSelected) {
                  e.currentTarget.style.borderColor = '#d1d5db';
                  e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.1)';
                  e.currentTarget.style.transform = 'translateY(-4px)';
                }
              }}
              onMouseLeave={(e) => {
                if (!isSelected) {
                  e.currentTarget.style.borderColor = '#e5e7eb';
                  e.currentTarget.style.boxShadow = '0 1px 3px rgba(0,0,0,0.1)';
                  e.currentTarget.style.transform = 'translateY(0)';
                }
              }}
            >
              {/* Image placeholder or actual image */}
              <div style={{
                width: '100%',
                height: '180px',
                backgroundColor: isSelected ? '#111827' : '#f3f4f6',
                backgroundImage: (room.heroImage || room.image) ? `url(${room.heroImage || room.image})` : 'none',
                backgroundSize: 'cover',
                backgroundPosition: 'center',
                position: 'relative',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                overflow: 'hidden'
              }}>
                {(room.heroImage || room.image) ? (
                  <>
                    {/* Image overlay for better text readability */}
                    <div style={{
                      position: 'absolute',
                      top: 0,
                      left: 0,
                      right: 0,
                      bottom: 0,
                      background: 'linear-gradient(to bottom, rgba(0,0,0,0) 0%, rgba(0,0,0,0.1) 100%)'
                    }}></div>
                    {isSelected && (
                      <div style={{
                        position: 'absolute',
                        top: '12px',
                        right: '12px',
                        backgroundColor: '#111827',
                        color: 'white',
                        padding: '6px 12px',
                        borderRadius: '20px',
                        fontSize: '12px',
                        fontWeight: '600',
                        letterSpacing: '0.5px',
                        zIndex: 10,
                        boxShadow: '0 2px 8px rgba(0,0,0,0.2)'
                      }}>
                        SELECTED
                      </div>
                    )}
                  </>
                ) : (
                  <>
                    <svg style={{ width: '40px', height: '40px', color: '#9ca3af' }} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                    </svg>
                    {isSelected && (
                      <div style={{
                        position: 'absolute',
                        top: '12px',
                        right: '12px',
                        backgroundColor: '#111827',
                        color: 'white',
                        padding: '6px 12px',
                        borderRadius: '20px',
                        fontSize: '12px',
                        fontWeight: '600',
                        letterSpacing: '0.5px'
                      }}>
                        SELECTED
                      </div>
                    )}
                  </>
                )}
              </div>
              
              <div style={{ padding: '20px' }}>
                <h3 style={{ 
                  fontSize: '20px', 
                  fontWeight: '600', 
                  marginBottom: '12px', 
                  color: '#111827',
                  fontFamily: "'Playfair Display', serif",
                  letterSpacing: '-0.01em',
                  lineHeight: '1.3'
                }}>
                  {room.name}
                </h3>
                
                {room.description && (
                  <p style={{ 
                    fontSize: '14px', 
                    color: '#6b7280', 
                    marginBottom: '16px', 
                    lineHeight: '1.6',
                    fontWeight: '300'
                  }}>
                    {room.description.substring(0, 120)}{room.description.length > 120 ? '...' : ''}
                  </p>
                )}
                
                <div style={{ 
                  display: 'flex', 
                  flexWrap: 'wrap',
                  gap: '8px', 
                  marginBottom: '16px',
                  paddingBottom: '16px',
                  borderBottom: '1px solid #f3f4f6'
                }}>
                  {room.maxOccupancy && (
                    <div style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '4px',
                      fontSize: '12px',
                      color: '#6b7280',
                      backgroundColor: '#f9fafb',
                      padding: '4px 10px',
                      borderRadius: '6px',
                      fontWeight: '400'
                    }}>
                      <svg style={{ width: '14px', height: '14px' }} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                      </svg>
                      Sleeps {room.maxOccupancy}
                    </div>
                  )}
                  {room.bedType && (
                    <div style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '4px',
                      fontSize: '12px',
                      color: '#6b7280',
                      backgroundColor: '#f9fafb',
                      padding: '4px 10px',
                      borderRadius: '6px',
                      fontWeight: '400'
                    }}>
                      <svg style={{ width: '14px', height: '14px' }} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                      </svg>
                      {room.bedType}
                    </div>
                  )}
                  {room.size && (
                    <div style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '4px',
                      fontSize: '12px',
                      color: '#6b7280',
                      backgroundColor: '#f9fafb',
                      padding: '4px 10px',
                      borderRadius: '6px',
                      fontWeight: '400'
                    }}>
                      <svg style={{ width: '14px', height: '14px' }} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
                      </svg>
                      {room.size}
                    </div>
                  )}
                </div>
                
                {room.priceFrom && (
                  <div style={{
                    display: 'flex',
                    alignItems: 'baseline',
                    gap: '4px'
                  }}>
                    <span style={{ 
                      fontSize: '12px', 
                      color: '#6b7280',
                      fontWeight: '400'
                    }}>
                      From
                    </span>
                    <span style={{ 
                      fontSize: '24px', 
                      fontWeight: '700', 
                      color: '#111827',
                      fontFamily: "'Playfair Display', serif"
                    }}>
                      £{room.priceFrom.toFixed(2)}
                    </span>
                    <span style={{ 
                      fontSize: '14px', 
                      color: '#6b7280',
                      fontWeight: '400'
                    }}>
                      /night
                    </span>
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
      <style>{`
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  );
};

