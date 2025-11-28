import type { Offer } from '../shared-types/domain.types';

export interface OfferCardProps {
  offer: Offer;
  className?: string;
}

export const OfferCard = ({ offer, className = '' }: OfferCardProps) => {
  return (
    <div className={`border rounded-lg p-6 shadow-md bg-gradient-to-r from-yellow-50 to-orange-50 ${className}`}>
      <h3 className="text-2xl font-bold mb-2">{offer.name}</h3>
      {offer.description && <p className="text-gray-700 mb-4">{offer.description}</p>}
      {offer.discount && (
        <div className="text-3xl font-bold text-orange-600 mb-2">
          {offer.discount}% OFF
        </div>
      )}
      {(offer.validFrom || offer.validTo) && (
        <p className="text-sm text-gray-600">
          Valid: {offer.validFrom} - {offer.validTo}
        </p>
      )}
    </div>
  );
};

