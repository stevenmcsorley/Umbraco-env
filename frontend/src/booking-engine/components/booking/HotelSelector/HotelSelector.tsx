import { useState, useEffect } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';

export interface Hotel {
  id: string;
  name: string;
  location?: string;
  city?: string;
  country?: string;
  description?: string;
  heroImage?: string;
  image?: string; // Keep for backward compatibility
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
        }}>Loading hotels...</p>
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

  if (hotels.length === 0) {
    return (
      <div style={{ 
        padding: '40px 20px', 
        textAlign: 'center', 
        color: '#6b7280' 
      }}>
        <p style={{ margin: 0, fontSize: '16px' }}>No hotels available</p>
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
          Select a Hotel
        </h2>
        <p style={{ 
          fontSize: '16px', 
          color: '#6b7280',
          margin: 0,
          fontWeight: '300'
        }}>
          Choose your destination to begin your booking
        </p>
      </div>
      <div style={{ 
        display: 'grid', 
        gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', 
        gap: '24px' 
      }}>
        {hotels.map((hotel) => {
          const isSelected = selectedHotelId === hotel.id;
          return (
            <div
              key={hotel.id}
              onClick={() => setSelectedHotelId(hotel.id)}
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
                height: '200px',
                backgroundColor: isSelected ? '#111827' : '#f3f4f6',
                backgroundImage: (hotel.heroImage || hotel.image) ? `url(${hotel.heroImage || hotel.image})` : 'none',
                backgroundSize: 'cover',
                backgroundPosition: 'center',
                position: 'relative',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                overflow: 'hidden'
              }}>
                {(hotel.heroImage || hotel.image) ? (
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
                    <svg style={{ width: '48px', height: '48px', color: '#9ca3af' }} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
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
                  marginBottom: '8px', 
                  color: '#111827',
                  fontFamily: "'Playfair Display', serif",
                  letterSpacing: '-0.01em',
                  lineHeight: '1.3'
                }}>
                  {hotel.name}
                </h3>
                {(hotel.location || hotel.city || hotel.country) && (
                  <div style={{ 
                    display: 'flex', 
                    alignItems: 'center', 
                    gap: '6px',
                    marginBottom: '12px'
                  }}>
                    <svg style={{ width: '16px', height: '16px', color: '#6b7280' }} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                    </svg>
                    <p style={{ 
                      fontSize: '14px', 
                      color: '#6b7280',
                      margin: 0,
                      fontWeight: '400'
                    }}>
                      {hotel.location || [hotel.city, hotel.country].filter(Boolean).join(', ')}
                    </p>
                  </div>
                )}
                {hotel.priceFrom && (
                  <div style={{
                    display: 'flex',
                    alignItems: 'baseline',
                    gap: '4px',
                    marginTop: '16px',
                    paddingTop: '16px',
                    borderTop: '1px solid #f3f4f6'
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
                      £{hotel.priceFrom.toFixed(2)}
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

