import { useState, useMemo } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';
import { useBookingFlow } from '../../../hooks/useBookingFlow';
import { useAvailability } from '../../../hooks/useAvailability';
import { useOffers } from '../../../hooks/useOffers';
import { useEvents } from '../../../hooks/useEvents';
import { Input } from '../../ui/Input';
import { Button } from '../../ui/Button';
import { TEST_IDS } from '../../../constants/testids';
import { AnalyticsManager } from '../../../../services/analytics';
import { formatPrice, calculateTotal, calculateAddOnPrice, calculateNights } from '../../../utils/priceUtils';
import { formatDate, isSameDay } from '../../../utils/dateUtils';

export interface BookingFormProps {
  className?: string;
  hotelId?: string;
  user?: {
    userId: string;
    email: string;
    firstName: string;
    lastName: string;
    phone?: string;
  } | null;
}

export const BookingForm = ({ className = '', hotelId, user }: BookingFormProps) => {
  const { selectedProductId, selectedDateRange, selectedAddOns, selectedEvents, availableAddOns, selectedOffer, setConfirmation, setError } = useBookingStore();
  const { submitBooking, loading } = useBookingFlow();
  
  const [firstName, setFirstName] = useState(user?.firstName || '');
  const [lastName, setLastName] = useState(user?.lastName || '');
  const [email, setEmail] = useState(user?.email || '');
  const [phone, setPhone] = useState(user?.phone || '');

  // Fetch availability for selected date range (same as AvailabilityPanel)
  const request = selectedProductId && selectedDateRange.from
    ? {
        productId: selectedProductId,
        from: selectedDateRange.from,
        to: selectedDateRange.to || selectedDateRange.from
      }
    : null;
  const { data: availabilityData } = useAvailability(request);
  
  // Fetch offers and events (needed for discount and event prices)
  const { offers } = useOffers(hotelId);
  const { events } = useEvents(hotelId);
  
  const selectedOfferDetails = selectedOffer && offers.find(o => o.id === selectedOffer.offerId);
  
  // Get selected event details
  const selectedEventDetails = useMemo(() => {
    if (!selectedDateRange.from || !events.length) return [];
    const endDate = selectedDateRange.to || selectedDateRange.from;
    const fromDate = new Date(selectedDateRange.from);
    fromDate.setHours(0, 0, 0, 0);
    const toDate = new Date(endDate);
    toDate.setHours(0, 0, 0, 0);
    
    return selectedEvents
      .map((se) => events.find((e) => e.id === se.eventId))
      .filter((event): event is typeof events[0] => {
        if (!event || !event.eventDate) return false;
        const eventDate = new Date(event.eventDate);
        eventDate.setHours(0, 0, 0, 0);
        return eventDate >= fromDate && eventDate <= toDate;
      });
  }, [selectedEvents, events, selectedDateRange]);

  const nights = useMemo(() => {
    if (!selectedDateRange.from) return 0;
    return calculateNights(selectedDateRange.from, selectedDateRange.to || selectedDateRange.from);
  }, [selectedDateRange]);

  // Calculate total price for summary - must match AvailabilityPanel logic exactly
  const totalPrice = useMemo(() => {
    // Use availabilityData (filtered to selected range) if available, otherwise return 0
    if (!availabilityData || !selectedDateRange.from) {
      return 0;
    }

    // Exclude check-out date from pricing (you don't pay for the day you check out)
    const checkOutDate = selectedDateRange.to ? new Date(selectedDateRange.to) : null;
    if (checkOutDate) checkOutDate.setHours(0, 0, 0, 0);
    
    const availableDays = availabilityData.days.filter((day) => {
      if (!day.available) return false;
      // Exclude check-out date
      if (checkOutDate) {
        const dayDate = typeof day.date === 'string' ? new Date(day.date) : day.date;
        dayDate.setHours(0, 0, 0, 0);
        if (isSameDay(dayDate, checkOutDate)) return false;
      }
      return true;
    });
    if (availableDays.length === 0) return 0;
    
    let baseTotal = calculateTotal(availableDays);
    
    // Calculate discount amount FIRST (based on original base price)
    let discountAmount = 0;
    if (selectedOfferDetails && selectedOfferDetails.discount) {
      discountAmount = (baseTotal * selectedOfferDetails.discount) / 100;
    }
    
    // Subtract discount from base
    const totalAfterDiscount = baseTotal - discountAmount;
    
    // Add event prices
    let eventsTotal = 0;
    selectedEventDetails.forEach((event) => {
      if (event && event.price) {
        eventsTotal += event.price;
      }
    });
    
    // Add add-ons
    let addOnsTotal = 0;
    selectedAddOns.forEach((selectedAddOn) => {
      const addOn = availableAddOns.find((a) => a.id === selectedAddOn.addOnId);
      if (addOn) {
        addOnsTotal += calculateAddOnPrice(
          addOn.price,
          addOn.type,
          selectedAddOn.quantity,
          nights,
          1
        );
      }
    });
    
    // Return: base - discount + events + addons (same as AvailabilityPanel)
    return totalAfterDiscount + addOnsTotal + eventsTotal;
  }, [availabilityData, selectedDateRange, selectedAddOns, selectedEvents, availableAddOns, nights, selectedOfferDetails, selectedEventDetails]);

  const handleSubmit = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();

    if (!selectedProductId || !selectedDateRange.from) {
      setError('Please select product and dates');
      return;
    }

    try {
      console.log('Submitting booking...', {
        productId: selectedProductId,
        from: selectedDateRange.from,
        to: selectedDateRange.to || selectedDateRange.from,
        userId: user?.userId,
        hasGuestDetails: !user,
        selectedEvents: selectedEvents,
        eventsCount: selectedEvents.length,
        selectedAddOns: selectedAddOns,
        addOnsCount: selectedAddOns.length
      });

      const booking = await submitBooking({
        productId: selectedProductId,
        from: selectedDateRange.from,
        to: selectedDateRange.to || selectedDateRange.from,
        userId: user?.userId,
        guestDetails: user ? undefined : {
          firstName,
          lastName,
          email,
          phone: phone || undefined
        },
        addOns: selectedAddOns.length > 0 ? selectedAddOns : undefined,
        events: selectedEvents.length > 0 ? selectedEvents : undefined
      });

      console.log('Booking successful:', booking);
      
      if (!booking || !booking.bookingId) {
        throw new Error('Invalid booking response: missing bookingId');
      }

      if (!booking.guestDetails) {
        console.warn('Booking response missing guestDetails, using fallback');
        booking.guestDetails = {
          firstName: user?.firstName || '',
          lastName: user?.lastName || '',
          email: user?.email || '',
          phone: user?.phone
        };
      }

      setConfirmation(booking);
      console.log('Confirmation set in store');
      AnalyticsManager.trackBookingConfirmed(booking.bookingId);
    } catch (err: any) {
      console.error('Booking error:', err);
      setError(err.message || 'Failed to create booking. Please try again.');
    }
  };

  const containerStyle: React.CSSProperties = {
    padding: '24px',
    border: '1px solid #e5e7eb',
    borderRadius: '12px',
    backgroundColor: '#ffffff',
    margin: '24px 0',
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
    letterSpacing: '-0.01em'
  };

  const inputStyle: React.CSSProperties = {
    width: '100%',
    padding: '12px 16px',
    border: '1px solid #e5e7eb',
    borderRadius: '6px',
    fontSize: '14px',
    color: '#111827',
    backgroundColor: '#ffffff',
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
    letterSpacing: '-0.01em',
    transition: 'all 0.15s ease',
    boxSizing: 'border-box'
  };

  const labelStyle: React.CSSProperties = {
    display: 'block',
    fontSize: '13px',
    fontWeight: '400',
    marginBottom: '8px',
    color: '#374151',
    letterSpacing: '0.01em'
  };

  const buttonStyle: React.CSSProperties = {
    width: '100%',
    padding: '14px 24px',
    backgroundColor: '#111827',
    color: '#ffffff',
    border: 'none',
    borderRadius: '6px',
    fontSize: '13px',
    fontWeight: '400',
    letterSpacing: '0.05em',
    textTransform: 'uppercase',
    cursor: 'pointer',
    transition: 'all 0.15s ease',
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif"
  };

  // If user is logged in, show summary instead of form
  if (user) {
    return (
      <div style={containerStyle} data-testid={TEST_IDS.bookingForm}>
        <h3 style={{
          fontSize: '20px',
          fontWeight: '500',
          marginBottom: '20px',
          color: '#111827',
          fontFamily: "'Playfair Display', serif",
          letterSpacing: '-0.02em'
        }}>
          Booking Summary
        </h3>
        
        <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
          {/* User Information */}
          <div style={{
            padding: '16px',
            backgroundColor: '#f9fafb',
            borderRadius: '8px',
            border: '1px solid #e5e7eb'
          }}>
            <h4 style={{
              fontSize: '14px',
              fontWeight: '500',
              marginBottom: '12px',
              color: '#374151',
              textTransform: 'uppercase',
              letterSpacing: '0.05em'
            }}>
              Booking For
            </h4>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              <p style={{ margin: 0, fontSize: '15px', color: '#111827', fontWeight: '400' }}>
                {user.firstName} {user.lastName}
              </p>
              <p style={{ margin: 0, fontSize: '14px', color: '#6b7280' }}>
                {user.email}
              </p>
              {user.phone && (
                <p style={{ margin: 0, fontSize: '14px', color: '#6b7280' }}>
                  {user.phone}
                </p>
              )}
            </div>
          </div>

          {/* Pricing Summary */}
          {selectedDateRange.from && (
            <div>
              <h4 style={{
                fontSize: '14px',
                fontWeight: '500',
                marginBottom: '12px',
                color: '#374151',
                textTransform: 'uppercase',
                letterSpacing: '0.05em'
              }}>
                Pricing Summary
              </h4>
              <div style={{
                display: 'flex',
                flexDirection: 'column',
                gap: '12px',
                padding: '16px',
                backgroundColor: '#ffffff',
                borderRadius: '8px',
                border: '1px solid #e5e7eb'
              }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ fontSize: '14px', color: '#6b7280' }}>
                    {(() => {
                      // Exclude check-out date from count (same logic as totalPrice calculation)
                      const checkOutDate = selectedDateRange.to ? new Date(selectedDateRange.to) : null;
                      if (checkOutDate) checkOutDate.setHours(0, 0, 0, 0);
                      const filteredDays = availabilityData?.days?.filter((day) => {
                        if (!day.available) return false;
                        if (checkOutDate) {
                          const dayDate = typeof day.date === 'string' ? new Date(day.date) : day.date;
                          dayDate.setHours(0, 0, 0, 0);
                          if (isSameDay(dayDate, checkOutDate)) return false;
                        }
                        return true;
                      }) || [];
                      const count = filteredDays.length;
                      return `${count} ${count === 1 ? 'day' : 'days'} available`;
                    })()}
                  </span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ fontSize: '14px', color: '#374151' }}>Base price:</span>
                  <span style={{ fontSize: '14px', color: '#111827', fontWeight: '400' }}>
                    {formatPrice(totalPrice, availabilityData?.currency || 'GBP')}
                  </span>
                </div>
                <div style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  paddingTop: '12px',
                  borderTop: '1px solid #e5e7eb',
                  marginTop: '4px'
                }}>
                  <span style={{ fontSize: '16px', color: '#111827', fontWeight: '500' }}>Total:</span>
                  <span style={{ fontSize: '16px', color: '#111827', fontWeight: '500' }}>
                    {formatPrice(totalPrice, availabilityData?.currency || 'GBP')}
                  </span>
                </div>
              </div>
            </div>
          )}

          {/* Confirm Button */}
          <button
            type="button"
            onClick={() => handleSubmit()}
            disabled={loading || !selectedProductId || !selectedDateRange.from}
            style={{
              ...buttonStyle,
              opacity: (loading || !selectedProductId || !selectedDateRange.from) ? 0.5 : 1,
              cursor: (loading || !selectedProductId || !selectedDateRange.from) ? 'not-allowed' : 'pointer'
            }}
            data-testid={TEST_IDS.submitButton}
            onMouseEnter={(e) => {
              if (!e.currentTarget.disabled) {
                e.currentTarget.style.backgroundColor = '#374151';
              }
            }}
            onMouseLeave={(e) => {
              if (!e.currentTarget.disabled) {
                e.currentTarget.style.backgroundColor = '#111827';
              }
            }}
          >
            {loading ? 'Processing...' : 'Confirm Booking'}
          </button>
        </div>
      </div>
    );
  }

  // If not logged in, show guest form
  return (
    <form onSubmit={handleSubmit} style={containerStyle} data-testid={TEST_IDS.bookingForm}>
      <h3 style={{
        fontSize: '20px',
        fontWeight: '500',
        marginBottom: '20px',
        color: '#111827',
        fontFamily: "'Playfair Display', serif",
        letterSpacing: '-0.02em'
      }}>
        Guest Information
      </h3>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
        <div>
          <label style={labelStyle}>First Name</label>
          <input
            type="text"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
            placeholder="John"
            required
            style={inputStyle}
            data-testid={TEST_IDS.firstNameInput}
            onFocus={(e) => {
              e.currentTarget.style.borderColor = '#111827';
              e.currentTarget.style.outline = 'none';
            }}
            onBlur={(e) => {
              e.currentTarget.style.borderColor = '#e5e7eb';
            }}
          />
        </div>
        <div>
          <label style={labelStyle}>Last Name</label>
          <input
            type="text"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
            placeholder="Doe"
            required
            style={inputStyle}
            data-testid={TEST_IDS.lastNameInput}
            onFocus={(e) => {
              e.currentTarget.style.borderColor = '#111827';
              e.currentTarget.style.outline = 'none';
            }}
            onBlur={(e) => {
              e.currentTarget.style.borderColor = '#e5e7eb';
            }}
          />
        </div>
        <div>
          <label style={labelStyle}>Email</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="john@example.com"
            required
            style={inputStyle}
            data-testid={TEST_IDS.emailInput}
            onFocus={(e) => {
              e.currentTarget.style.borderColor = '#111827';
              e.currentTarget.style.outline = 'none';
            }}
            onBlur={(e) => {
              e.currentTarget.style.borderColor = '#e5e7eb';
            }}
          />
        </div>
        <div>
          <label style={labelStyle}>Phone (optional)</label>
          <input
            type="tel"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            placeholder="+44 20 1234 5678"
            style={inputStyle}
            data-testid={TEST_IDS.phoneInput}
            onFocus={(e) => {
              e.currentTarget.style.borderColor = '#111827';
              e.currentTarget.style.outline = 'none';
            }}
            onBlur={(e) => {
              e.currentTarget.style.borderColor = '#e5e7eb';
            }}
          />
        </div>
        <button
          type="submit"
          disabled={loading || !selectedProductId || !selectedDateRange.from}
          style={{
            ...buttonStyle,
            opacity: (loading || !selectedProductId || !selectedDateRange.from) ? 0.5 : 1,
            cursor: (loading || !selectedProductId || !selectedDateRange.from) ? 'not-allowed' : 'pointer'
          }}
          data-testid={TEST_IDS.submitButton}
          onMouseEnter={(e) => {
            if (!e.currentTarget.disabled) {
              e.currentTarget.style.backgroundColor = '#374151';
            }
          }}
          onMouseLeave={(e) => {
            if (!e.currentTarget.disabled) {
              e.currentTarget.style.backgroundColor = '#111827';
            }
          }}
        >
          {loading ? 'Processing...' : 'Confirm Booking'}
        </button>
      </div>
    </form>
  );
};

