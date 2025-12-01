import { useState, useEffect, FormEvent } from 'react';
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
  const { selectedHotelId, setSelectedHotelId, setSelectedDateRange } = useBookingStore();
  const [hotels, setHotels] = useState<Hotel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isSearching, setIsSearching] = useState(false);

  const [searchTown, setSearchTown] = useState('');
  const [searchFrom, setSearchFrom] = useState('');
  const [searchTo, setSearchTo] = useState('');
  const [searchGuests, setSearchGuests] = useState('2');

  const loadHotels = async (url: string) => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch(url);
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

  useEffect(() => {
    // Initial load: all hotels
    loadHotels('/api/hotels');
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSearch = async (e: FormEvent) => {
    e.preventDefault();

    const params = new URLSearchParams();
    if (searchTown.trim()) {
      params.append('town', searchTown.trim());
    }
    if (searchFrom) {
      params.append('from', searchFrom);
    }
    if (searchTo) {
      params.append('to', searchTo);
    }
    if (searchGuests) {
      params.append('guests', searchGuests);
    }

    // If dates are provided, also push into booking store so the calendar is pre-filled
    if (searchFrom) {
      const fromDate = new Date(searchFrom);
      const toDate = searchTo ? new Date(searchTo) : new Date(searchFrom);
      setSelectedDateRange(fromDate, toDate);
    }

    const url = params.toString()
      ? `/api/hotels/search?${params.toString()}`
      : '/api/hotels';

    setIsSearching(true);
    await loadHotels(url);
    setIsSearching(false);
  };

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
      {/* Search panel */}
      <div
        style={{
          marginBottom: '32px',
          padding: '20px',
          borderRadius: '16px',
          background: 'linear-gradient(135deg, #f9fafb 0%, #f3f4f6 100%)',
          border: '1px solid #e5e7eb',
          boxShadow: '0 10px 30px rgba(15, 23, 42, 0.08)',
        }}
      >
        <h2 style={{ 
          fontSize: '28px', 
          fontWeight: '600', 
          marginBottom: '4px', 
          color: '#111827',
          fontFamily: "'Playfair Display', serif",
          letterSpacing: '-0.02em'
        }}>
          Plan your stay
        </h2>
        <p style={{ 
          fontSize: '16px', 
          color: '#6b7280',
          margin: 0,
          fontWeight: '300',
          marginBottom: '20px'
        }}>
          Search by town, dates, and number of guests to see available hotels.
        </p>

        <form
          onSubmit={handleSearch}
          style={{
            display: 'grid',
            gridTemplateColumns: 'minmax(0, 2fr) repeat(3, minmax(0, 1.3fr)) minmax(0, 1.3fr)',
            gap: '12px',
            alignItems: 'end',
          }}
        >
          {/* Destination */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '12px', textTransform: 'uppercase', letterSpacing: '0.08em', color: '#6b7280', fontWeight: 500 }}>
              Destination / Town
            </label>
            <input
              type="text"
              value={searchTown}
              onChange={(e) => setSearchTown(e.target.value)}
              placeholder="e.g. Edinburgh, London"
              style={{
                padding: '10px 12px',
                borderRadius: '999px',
                border: '1px solid #d1d5db',
                fontSize: '14px',
                outline: 'none',
                width: '100%',
              }}
            />
          </div>

          {/* Check-in */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '12px', textTransform: 'uppercase', letterSpacing: '0.08em', color: '#6b7280', fontWeight: 500 }}>
              Check-in
            </label>
            <input
              type="date"
              value={searchFrom}
              onChange={(e) => setSearchFrom(e.target.value)}
              style={{
                padding: '10px 12px',
                borderRadius: '999px',
                border: '1px solid #d1d5db',
                fontSize: '14px',
                outline: 'none',
                width: '100%',
              }}
            />
          </div>

          {/* Check-out */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '12px', textTransform: 'uppercase', letterSpacing: '0.08em', color: '#6b7280', fontWeight: 500 }}>
              Check-out
            </label>
            <input
              type="date"
              value={searchTo}
              onChange={(e) => setSearchTo(e.target.value)}
              style={{
                padding: '10px 12px',
                borderRadius: '999px',
                border: '1px solid #d1d5db',
                fontSize: '14px',
                outline: 'none',
                width: '100%',
              }}
            />
          </div>

          {/* Guests */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            <label style={{ fontSize: '12px', textTransform: 'uppercase', letterSpacing: '0.08em', color: '#6b7280', fontWeight: 500 }}>
              Guests
            </label>
            <select
              value={searchGuests}
              onChange={(e) => setSearchGuests(e.target.value)}
              style={{
                padding: '10px 12px',
                borderRadius: '999px',
                border: '1px solid #d1d5db',
                fontSize: '14px',
                outline: 'none',
                width: '100%',
                backgroundColor: 'white',
              }}
            >
              <option value="1">1 guest</option>
              <option value="2">2 guests</option>
              <option value="3">3 guests</option>
              <option value="4">4 guests</option>
              <option value="5">5 guests</option>
            </select>
          </div>

          {/* Submit */}
          <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
            <button
              type="submit"
              style={{
                padding: '12px 22px',
                borderRadius: '999px',
                border: 'none',
                backgroundColor: '#111827',
                color: 'white',
                fontSize: '14px',
                fontWeight: 500,
                textTransform: 'uppercase',
                letterSpacing: '0.12em',
                cursor: 'pointer',
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                gap: '8px',
                minWidth: '140px',
              }}
            >
              {isSearching ? 'Searching...' : 'Search'}
            </button>
          </div>
        </form>
      </div>

      <div style={{ marginBottom: '24px' }}>
        <h3 style={{ 
          fontSize: '20px', 
          fontWeight: '600', 
          marginBottom: '4px', 
          color: '#111827',
          fontFamily: "'Playfair Display', serif",
          letterSpacing: '-0.01em'
        }}>
          Select a Hotel
        </h3>
        <p style={{ 
          fontSize: '14px', 
          color: '#6b7280',
          margin: 0,
          fontWeight: '300'
        }}>
          {isSearching
            ? 'Searching for available hotels...'
            : `Showing ${hotels.length} hotel${hotels.length === 1 ? '' : 's'}${searchTown ? ` in “${searchTown.trim()}”` : ''}`}
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

