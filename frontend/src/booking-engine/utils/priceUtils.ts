export const formatPrice = (price: number | undefined, currency: string = 'GBP'): string => {
  if (price === undefined) return 'N/A';
  return new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency
  }).format(price);
};

export const calculateTotal = (days: Array<{ price?: number }>): number => {
  return days.reduce((total, day) => total + (day.price || 0), 0);
};

export const calculateAddOnPrice = (
  addOnPrice: number,
  addOnType: 'one-time' | 'per-night' | 'per-person' | 'per-unit',
  quantity: number,
  nights: number = 1,
  guests: number = 1
): number => {
  switch (addOnType) {
    case 'one-time':
      return addOnPrice * quantity;
    case 'per-night':
      return addOnPrice * quantity * nights;
    case 'per-person':
      return addOnPrice * quantity * guests;
    case 'per-unit':
      return addOnPrice * quantity;
    default:
      return addOnPrice * quantity;
  }
};

export const calculateNights = (from: Date | string, to: Date | string): number => {
  const fromDate = typeof from === 'string' ? new Date(from) : from;
  const toDate = typeof to === 'string' ? new Date(to) : to;
  // For single day bookings (same date), return 1 night (or 0 if you prefer)
  // Most hotels charge for 1 night even for same-day check-in/check-out
  const nights = Math.ceil((toDate.getTime() - fromDate.getTime()) / (1000 * 60 * 60 * 24));
  return nights === 0 ? 1 : nights; // Single day = 1 night
};

