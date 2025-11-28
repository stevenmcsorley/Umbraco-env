import { useState } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';
import { useBookingFlow } from '../../../hooks/useBookingFlow';
import { Input } from '../../ui/Input';
import { Button } from '../../ui/Button';
import { TEST_IDS } from '../../../constants/testids';
import { AnalyticsManager } from '../../../../services/analytics';

export interface BookingFormProps {
  className?: string;
}

export const BookingForm = ({ className = '' }: BookingFormProps) => {
  const { selectedProductId, selectedDateRange, setConfirmation, setError } = useBookingStore();
  const { submitBooking, loading } = useBookingFlow();
  
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!selectedProductId || !selectedDateRange.from || !selectedDateRange.to) {
      setError('Please select product and dates');
      return;
    }

    try {
      const booking = await submitBooking({
        productId: selectedProductId,
        from: selectedDateRange.from,
        to: selectedDateRange.to,
        guestDetails: {
          firstName,
          lastName,
          email,
          phone: phone || undefined
        }
      });

      setConfirmation(booking);
      AnalyticsManager.trackBookingConfirmed(booking.bookingId);
    } catch (err: any) {
      setError(err.message);
    }
  };

  return (
    <form onSubmit={handleSubmit} className={className} data-testid={TEST_IDS.bookingForm}>
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1">First Name</label>
          <Input
            value={firstName}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFirstName(e.target.value)}
            placeholder="John"
            required
            data-testid={TEST_IDS.firstNameInput}
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">Last Name</label>
          <Input
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
            placeholder="Doe"
            required
            data-testid={TEST_IDS.lastNameInput}
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">Email</label>
          <Input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="john@example.com"
            required
            data-testid={TEST_IDS.emailInput}
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">Phone (optional)</label>
          <Input
            type="tel"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            placeholder="+44 20 1234 5678"
            data-testid={TEST_IDS.phoneInput}
          />
        </div>
        <Button
          type="submit"
          disabled={loading || !selectedProductId || !selectedDateRange.from || !selectedDateRange.to}
          data-testid={TEST_IDS.submitButton}
        >
          {loading ? 'Processing...' : 'Confirm Booking'}
        </Button>
      </div>
    </form>
  );
};

