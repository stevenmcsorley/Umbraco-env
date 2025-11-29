import { useState, useEffect } from 'react';

export interface Event {
  id: string;
  name: string;
  slug: string;
  description?: string;
  eventDate?: string;
  price?: number;
  location?: string;
  image?: string;
  url: string;
}

export const useEvents = (hotelId: string | null | undefined) => {
  const [events, setEvents] = useState<Event[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!hotelId) {
      setEvents([]);
      return;
    }

    setLoading(true);
    setError(null);

    fetch(`/api/hotels/${hotelId}/events`)
      .then(r => {
        if (!r.ok) {
          throw new Error('Failed to fetch events');
        }
        return r.json();
      })
      .then(data => {
        setEvents(Array.isArray(data) ? data : []);
      })
      .catch((err) => {
        setError(err.message);
        setEvents([]);
      })
      .finally(() => setLoading(false));
  }, [hotelId]);

  return { events, loading, error };
};

