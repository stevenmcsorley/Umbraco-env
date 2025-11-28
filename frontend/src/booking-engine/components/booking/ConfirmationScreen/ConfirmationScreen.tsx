import { useBookingStore } from '../../../app/state/bookingStore';
import { TEST_IDS } from '../../../constants/testids';
import { Card } from '../../ui/Card';
import { Button } from '../../ui/Button';

export interface ConfirmationScreenProps {
  className?: string;
}

export const ConfirmationScreen = ({ className = '' }: ConfirmationScreenProps) => {
  const { confirmation, reset } = useBookingStore();

  if (!confirmation) return null;

  return (
    <Card className={className} data-testid={TEST_IDS.confirmationScreen}>
      <h2 className="text-2xl font-bold mb-4">Booking Confirmed!</h2>
      <div className="space-y-2 mb-6">
        <p>
          <strong>Booking Reference:</strong>{' '}
          <span data-testid={TEST_IDS.confirmationReference}>{confirmation.bookingId}</span>
        </p>
        <p>
          <strong>Product ID:</strong> {confirmation.productId}
        </p>
        <p>
          <strong>From:</strong> {typeof confirmation.from === 'string' ? confirmation.from : confirmation.from.toLocaleDateString()}
        </p>
        <p>
          <strong>To:</strong> {typeof confirmation.to === 'string' ? confirmation.to : confirmation.to.toLocaleDateString()}
        </p>
        <p>
          <strong>Guest:</strong> {confirmation.guestDetails.firstName} {confirmation.guestDetails.lastName}
        </p>
        <p>
          <strong>Email:</strong> {confirmation.guestDetails.email}
        </p>
        <p>
          <strong>Status:</strong> {confirmation.status}
        </p>
      </div>
      <Button onClick={reset}>Book Another</Button>
    </Card>
  );
};

