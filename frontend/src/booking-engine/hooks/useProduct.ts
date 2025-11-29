import { useState, useEffect } from 'react';

export interface RoomDetails {
  id: string;
  name: string;
  description?: string;
  heroImage?: string;
  images?: string[];
  features?: string[];
  priceFrom?: number;
}

export interface HotelDetails {
  id: string;
  name: string;
  description?: string;
  address?: string;
  city?: string;
  country?: string;
}

export interface ProductDetails {
  room?: RoomDetails;
  hotel?: HotelDetails;
}

export const useProduct = (productId: string | null, hotelId?: string) => {
  const [productDetails, setProductDetails] = useState<ProductDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!productId) {
      setProductDetails(null);
      return;
    }

    setLoading(true);
    setError(null);

    // Fetch room details
    const fetchRoom = fetch(`/api/hotels/${hotelId || 'unknown'}/rooms`)
      .then(r => r.ok ? r.json() : null)
      .then(rooms => {
        if (rooms && Array.isArray(rooms)) {
          return rooms.find((r: any) => r.id === productId);
        }
        return null;
      })
      .catch(() => null);

    // Fetch hotel details
    const fetchHotel = hotelId
      ? fetch(`/api/hotels/${hotelId}`)
          .then(r => r.ok ? r.json() : null)
          .catch(() => null)
      : Promise.resolve(null);

    Promise.all([fetchRoom, fetchHotel])
      .then(([room, hotel]) => {
        // Normalize room features - convert string to array if needed
        let normalizedRoom = room;
        if (room && room.features) {
          if (typeof room.features === 'string') {
            // Split by newlines or commas and filter empty strings
            normalizedRoom = {
              ...room,
              features: room.features
                .split(/\n|,/)
                .map((f: string) => f.trim())
                .filter((f: string) => f.length > 0)
            };
          } else if (!Array.isArray(room.features)) {
            // If it's not an array and not a string, remove it
            normalizedRoom = {
              ...room,
              features: undefined
            };
          }
        }
        
        setProductDetails({
          room: normalizedRoom || undefined,
          hotel: hotel || undefined
        });
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [productId, hotelId]);

  return { productDetails, loading, error };
};

