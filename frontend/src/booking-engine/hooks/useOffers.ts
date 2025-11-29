import { useState, useEffect } from 'react';

export interface Offer {
  id: string;
  name: string;
  slug: string;
  description?: string;
  discount?: number;
  validFrom?: string;
  validTo?: string;
  minNights?: number;
  minAdvanceBookingDays?: number;
  image?: string;
  url: string;
}

export const useOffers = (hotelId: string | null | undefined) => {
  const [offers, setOffers] = useState<Offer[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!hotelId) {
      setOffers([]);
      return;
    }

    setLoading(true);
    setError(null);

    fetch(`/api/hotels/${hotelId}/offers`)
      .then(r => {
        if (!r.ok) {
          throw new Error('Failed to fetch offers');
        }
        return r.json();
      })
      .then(data => {
        setOffers(Array.isArray(data) ? data : []);
      })
      .catch((err) => {
        setError(err.message);
        setOffers([]);
      })
      .finally(() => setLoading(false));
  }, [hotelId]);

  return { offers, loading, error };
};

