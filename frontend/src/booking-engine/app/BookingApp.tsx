import { useEffect } from 'react';
import { useBookingStore } from './state/bookingStore';
import { PageContainer } from '../components/layout/PageContainer';
import { PageSection } from '../components/layout/PageSection';
import { CalendarSelector } from '../components/booking/CalendarSelector';
import { AvailabilityPanel } from '../components/booking/AvailabilityPanel';
import { AddOnsSelector } from '../components/booking/AddOnsSelector';
import { BookingForm } from '../components/booking/BookingForm';
import { ConfirmationScreen } from '../components/booking/ConfirmationScreen';
import { AnalyticsManager } from '../../services/analytics';
import type { DesignTokens } from '../constants/tokens';

export interface BookingAppProps {
  hotelId?: string;
  tokens?: Partial<DesignTokens>;
  apiBaseUrl?: string;
  defaultProductId?: string;
  user?: {
    userId: string;
    email: string;
    firstName: string;
    lastName: string;
    phone?: string;
  } | null;
}

export const BookingApp = ({
  hotelId,
  tokens: _tokens = {},
  apiBaseUrl: _apiBaseUrl = '/engine',
  defaultProductId,
  user
}: BookingAppProps) => {
  const { selectedProductId, setSelectedProductId, confirmation } = useBookingStore();

  useEffect(() => {
    if (defaultProductId && !selectedProductId) {
      setSelectedProductId(defaultProductId);
      AnalyticsManager.trackBookingStart(hotelId || '', defaultProductId);
    }
  }, [defaultProductId, selectedProductId, hotelId, setSelectedProductId]);

  if (confirmation) {
    return (
      <PageContainer>
        <ConfirmationScreen />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageSection>
        <h1 className="text-2xl font-bold mb-4">Book Your Stay</h1>
        <CalendarSelector hotelId={hotelId} />
      </PageSection>

        <PageSection>
          <AvailabilityPanel hotelId={hotelId} />
        </PageSection>

        {hotelId && (
          <PageSection>
            <AddOnsSelector hotelId={hotelId} />
          </PageSection>
        )}

        <PageSection>
          <BookingForm user={user} />
        </PageSection>
    </PageContainer>
  );
};

