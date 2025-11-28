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

