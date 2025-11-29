import { useEffect } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';
import { Card } from '../../ui/Card';
import { Button } from '../../ui/Button';
import { TEST_IDS } from '../../../constants/testids';
import { formatPrice } from '../../../utils/priceUtils';
import type { AddOn } from '../../../types/domain.types';

export interface AddOnsSelectorProps {
  hotelId?: string;
  className?: string;
}

export const AddOnsSelector = ({ hotelId, className = '' }: AddOnsSelectorProps) => {
  const { availableAddOns, selectedAddOns, setAvailableAddOns, toggleAddOn } = useBookingStore();

  useEffect(() => {
    // Fetch add-ons when hotelId is available
    if (hotelId) {
      // TODO: Fetch from API when endpoint is available
      // For now, use empty array - will be populated when Umbraco add-ons content is created
      setAvailableAddOns([]);
    }
  }, [hotelId, setAvailableAddOns]);

  if (!availableAddOns || availableAddOns.length === 0) {
    return null;
  }

  const getSelectedQuantity = (addOnId: string): number => {
    const selected = selectedAddOns.find(a => a.addOnId === addOnId);
    return selected?.quantity || 0;
  };

  const isSelected = (addOnId: string): boolean => {
    return getSelectedQuantity(addOnId) > 0;
  };

  return (
    <div className={className} data-testid={TEST_IDS.addOnsSelector}>
      <h3 className="text-lg font-semibold mb-4">Add-ons</h3>
      <div className="space-y-3">
        {availableAddOns.map((addOn) => {
          const quantity = getSelectedQuantity(addOn.id);
          const selected = isSelected(addOn.id);

          return (
            <Card key={addOn.id} className="p-4" data-testid={TEST_IDS.addOnCard(addOn.id)}>
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <h4 className="font-medium mb-1">{addOn.name}</h4>
                  {addOn.description && (
                    <p className="text-sm text-gray-600 mb-2">{addOn.description}</p>
                  )}
                  <p className="text-sm font-semibold">
                    {formatPrice(addOn.price, 'GBP')}
                    {addOn.type !== 'one-time' && (
                      <span className="text-gray-500 font-normal ml-1">
                        ({addOn.type === 'per-night' ? 'per night' : 
                          addOn.type === 'per-person' ? 'per person' : 
                          'per unit'})
                      </span>
                    )}
                  </p>
                </div>
                <div className="ml-4 flex items-center gap-2">
                  {selected && (
                    <>
                      <Button
                        variant="outline"
                        onClick={() => toggleAddOn(addOn.id, Math.max(0, quantity - 1))}
                        data-testid={TEST_IDS.addOnDecrease(addOn.id)}
                      >
                        -
                      </Button>
                      <span className="w-8 text-center" data-testid={TEST_IDS.addOnQuantity(addOn.id)}>
                        {quantity}
                      </span>
                      <Button
                        variant="outline"
                        onClick={() => toggleAddOn(addOn.id, quantity + 1)}
                        data-testid={TEST_IDS.addOnIncrease(addOn.id)}
                      >
                        +
                      </Button>
                    </>
                  )}
                  {!selected && (
                    <Button
                      variant="primary"
                      onClick={() => toggleAddOn(addOn.id, 1)}
                      data-testid={TEST_IDS.addOnAdd(addOn.id)}
                    >
                      Add
                    </Button>
                  )}
                </div>
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
};

