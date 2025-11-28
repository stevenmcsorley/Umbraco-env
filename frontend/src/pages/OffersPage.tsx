import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { hotelsApi } from '../services/api/hotelsApi';
import type { Offer } from '../shared-types/domain.types';
import { OfferCard } from '../components/OfferCard';

export const OffersPage = () => {
  const { id } = useParams<{ id: string }>();
  const [offers, setOffers] = useState<Offer[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;

    hotelsApi.getOffers(id)
      .then(setOffers)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="p-8 text-center">Loading...</div>;

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-4xl font-bold mb-8">Special Offers</h1>
      {offers.length === 0 ? (
        <p className="text-gray-600">No offers available at this time.</p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {offers.map((offer) => (
            <OfferCard key={offer.id} offer={offer} />
          ))}
        </div>
      )}
    </div>
  );
};

