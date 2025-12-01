import { useEffect } from 'react';
import { useBookingStore } from './state/bookingStore';
import { PageContainer } from '../components/layout/PageContainer';
import { PageSection } from '../components/layout/PageSection';
import { CalendarSelector } from '../components/booking/CalendarSelector';
import { AvailabilityPanel } from '../components/booking/AvailabilityPanel';
import { AddOnsSelector } from '../components/booking/AddOnsSelector';
import { BookingForm } from '../components/booking/BookingForm';
import { ConfirmationScreen } from '../components/booking/ConfirmationScreen';
import { HotelSelector } from '../components/booking/HotelSelector/HotelSelector';
import { RoomSelector } from '../components/booking/RoomSelector/RoomSelector';
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
  hotelId: propHotelId,
  tokens: _tokens = {},
  apiBaseUrl: _apiBaseUrl = '/engine',
  defaultProductId,
  user
}: BookingAppProps) => {
  const { selectedHotelId, selectedProductId, setSelectedProductId, setSelectedHotelId, confirmation } = useBookingStore();
  
  // Use prop hotelId if provided, otherwise use selectedHotelId from store
  const hotelId = propHotelId || selectedHotelId;

  useEffect(() => {
    // If hotelId is provided as prop, set it in store
    if (propHotelId && !selectedHotelId) {
      setSelectedHotelId(propHotelId);
    }
    
    // If defaultProductId is provided, set it
    if (defaultProductId && !selectedProductId) {
      setSelectedProductId(defaultProductId);
      AnalyticsManager.trackBookingStart(hotelId || '', defaultProductId);
    }
  }, [defaultProductId, selectedProductId, selectedHotelId, propHotelId, hotelId, setSelectedProductId, setSelectedHotelId]);

  if (confirmation) {
    return (
      <PageContainer>
        <ConfirmationScreen />
      </PageContainer>
    );
  }

  // Global booking flow: show hotel selector if no hotel selected
  const isGlobalBooking = !propHotelId;
  const showHotelSelector = isGlobalBooking && !selectedHotelId;
  const showRoomSelector = isGlobalBooking && selectedHotelId && !selectedProductId;
  const showBookingFlow = selectedProductId || propHotelId;

  return (
    <PageContainer>
      <PageSection>
        <h1 className="text-2xl font-bold mb-4">Book Your Stay</h1>
        
        {/* Step 1: Select Hotel (only for global booking) */}
        {showHotelSelector && (
          <div style={{ marginBottom: '32px' }}>
            <HotelSelector />
          </div>
        )}
        
        {/* Step 2: Select Room (only for global booking) */}
        {showRoomSelector && selectedHotelId && (
          <div style={{ marginBottom: '32px' }}>
            <RoomSelector hotelId={selectedHotelId} />
          </div>
        )}
        
        {/* Step 3: Booking Flow (calendar, availability, booking form) */}
        {showBookingFlow && (
          <>
            <CalendarSelector hotelId={hotelId} />
          </>
        )}
      </PageSection>

      {showBookingFlow && (
        <>
          <PageSection>
            <AvailabilityPanel hotelId={hotelId} />
          </PageSection>

          {hotelId && (
            <PageSection>
              <AddOnsSelector hotelId={hotelId} />
            </PageSection>
          )}

          <PageSection>
            <BookingForm user={user} hotelId={hotelId} />
          </PageSection>
        </>
      )}
      
      {/* Show message if no hotel/room selected in global booking */}
      {isGlobalBooking && !selectedHotelId && !showHotelSelector && (
        <PageSection>
          <div style={{ padding: '40px', textAlign: 'center', color: '#6b7280' }}>
            <p>Please select a hotel to continue</p>
          </div>
        </PageSection>
      )}
      
      {isGlobalBooking && selectedHotelId && !selectedProductId && !showRoomSelector && (
        <PageSection>
          <div style={{ padding: '40px', textAlign: 'center', color: '#6b7280' }}>
            <p>Please select a room to continue</p>
          </div>
        </PageSection>
      )}
    </PageContainer>
  );
};

