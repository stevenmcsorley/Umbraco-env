import { useEffect } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';
import { useAvailability } from '../../../hooks/useAvailability';
import { TEST_IDS } from '../../../constants/testids';
import { Spinner } from '../../ui/Spinner';
import { formatPrice, calculateTotal } from '../../../utils/priceUtils';

export interface AvailabilityPanelProps {
  className?: string;
}

export const AvailabilityPanel = ({ className = '' }: AvailabilityPanelProps) => {
  const { selectedProductId, selectedDateRange, setAvailabilityResult } = useBookingStore();
  
  const request = selectedProductId && selectedDateRange.from && selectedDateRange.to
    ? {
        productId: selectedProductId,
        from: selectedDateRange.from,
        to: selectedDateRange.to
      }
    : null;

  const { data, loading, error } = useAvailability(request);

  useEffect(() => {
    if (request && data) {
      setAvailabilityResult(data);
    }
  }, [data, request, setAvailabilityResult]);

  if (!request) {
    return (
      <div className={className} data-testid={TEST_IDS.availabilityPanel}>
        <p className="text-gray-600">Please select dates to check availability</p>
      </div>
    );
  }

  if (loading) {
    return (
      <div className={className} data-testid={TEST_IDS.availabilityPanel}>
        <Spinner />
        <p className="mt-2">Checking availability...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className={className} data-testid={TEST_IDS.availabilityPanel}>
        <p className="text-red-600">Error: {error}</p>
      </div>
    );
  }

  if (!data) {
    return null;
  }

  const availableDays = data.days.filter((day) => day.available);
  const totalPrice = calculateTotal(availableDays);

  return (
    <div className={className} data-testid={TEST_IDS.availabilityPanel}>
      <h3 className="text-lg font-semibold mb-4">Availability</h3>
      {availableDays.length > 0 ? (
        <div>
          <p className="mb-2">
            {availableDays.length} day{availableDays.length !== 1 ? 's' : ''} available
          </p>
          <p className="text-xl font-bold mb-4">
            Total: {formatPrice(totalPrice, data.currency)}
          </p>
        </div>
      ) : (
        <p className="text-gray-600">No availability for selected dates</p>
      )}
    </div>
  );
};

