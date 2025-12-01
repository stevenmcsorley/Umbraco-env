import { useEffect, useMemo } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';
import { useAvailability } from '../../../hooks/useAvailability';
import { useProduct } from '../../../hooks/useProduct';
import { useOffers } from '../../../hooks/useOffers';
import { useEvents } from '../../../hooks/useEvents';
import { TEST_IDS } from '../../../constants/testids';
import { Spinner } from '../../ui/Spinner';
import { formatPrice, calculateTotal, calculateAddOnPrice, calculateNights } from '../../../utils/priceUtils';
import { formatDate, isSameDay } from '../../../utils/dateUtils';
import type { AddOn } from '../../../types/domain.types';

export interface AvailabilityPanelProps {
  className?: string;
  hotelId?: string;
}

export const AvailabilityPanel = ({ className = '', hotelId }: AvailabilityPanelProps) => {
  const { selectedProductId, selectedDateRange, setAvailabilityResult, selectedAddOns, availableAddOns, selectedOffer, setSelectedOffer, selectedEvents, toggleEvent } = useBookingStore();
  
  // Fetch product (room) and hotel details
  const { productDetails, loading: productLoading } = useProduct(selectedProductId, hotelId);
  
  // Fetch offers and events for this hotel
  const { offers } = useOffers(hotelId);
  const { events } = useEvents(hotelId);
  
  // Find selected offer details
  const selectedOfferDetails = selectedOffer && offers.find(o => o.id === selectedOffer.offerId);
  
  // Automatically remove events that are no longer within the selected date range
  useEffect(() => {
    if (!selectedDateRange.from || !events.length || !selectedEvents.length) return;
    
    const endDate = selectedDateRange.to || selectedDateRange.from;
    const fromDate = new Date(selectedDateRange.from);
    fromDate.setHours(0, 0, 0, 0);
    const toDate = new Date(endDate);
    toDate.setHours(0, 0, 0, 0);
    
    // Find events that are outside the selected date range
    const eventsToRemove = selectedEvents.filter((se) => {
      const event = events.find((e) => e.id === se.eventId);
      if (!event || !event.eventDate) return false;
      const eventDate = new Date(event.eventDate);
      eventDate.setHours(0, 0, 0, 0);
      return eventDate < fromDate || eventDate > toDate;
    });
    
    // Remove events that are outside the date range
    eventsToRemove.forEach((se) => {
      toggleEvent(se.eventId);
    });
  }, [selectedDateRange, events, selectedEvents, toggleEvent]);
  
  // Find events that fall on selected dates
  const eventsOnSelectedDates = useMemo(() => {
    if (!selectedDateRange.from || !events.length) return [];
    
    const endDate = selectedDateRange.to || selectedDateRange.from;
    const fromDate = new Date(selectedDateRange.from);
    fromDate.setHours(0, 0, 0, 0);
    const toDate = new Date(endDate);
    toDate.setHours(0, 0, 0, 0);
    
    return events.filter(event => {
      if (!event.eventDate) return false;
      const eventDate = new Date(event.eventDate);
      eventDate.setHours(0, 0, 0, 0);
      
      // Check if event date falls within selected range
      return eventDate >= fromDate && eventDate <= toDate;
    });
  }, [selectedDateRange, events]);
  
  // Get selected event details - only include events that fall within selected dates
  const selectedEventDetails = useMemo(() => {
    if (!selectedDateRange.from || !events.length) return [];
    
    const endDate = selectedDateRange.to || selectedDateRange.from;
    const fromDate = new Date(selectedDateRange.from);
    fromDate.setHours(0, 0, 0, 0);
    const toDate = new Date(endDate);
    toDate.setHours(0, 0, 0, 0);
    
    return selectedEvents
      .map((se: { eventId: string }) => events.find((e) => e.id === se.eventId))
      .filter((event): event is typeof events[0] => {
        if (!event || !event.eventDate) return false;
        const eventDate = new Date(event.eventDate);
        eventDate.setHours(0, 0, 0, 0);
        // Only include events that fall within the selected date range
        return eventDate >= fromDate && eventDate <= toDate;
      });
  }, [selectedEvents, events, selectedDateRange]);
  
  // Handle single day selection: if only 'from' is set, treat it as a single day (from to from)
  const request = selectedProductId && selectedDateRange.from
    ? {
        productId: selectedProductId,
        from: selectedDateRange.from,
        to: selectedDateRange.to || selectedDateRange.from // Use 'to' if set, otherwise use 'from' for single day
      }
    : null;

  const { data, loading, error } = useAvailability(request);

  // Don't update the store here - AvailabilityPanel uses its own fetched data for calculations
  // The calendar's wide-range availability should remain in the store
  // This prevents infinite loops when dates are selected

  // Calculate total including add-ons, offers, and events
  const totalWithAddOns = useMemo(() => {
    if (!data || !selectedDateRange.from) return null;

    // Exclude check-out date from pricing (you don't pay for the day you check out)
    const checkOutDate = selectedDateRange.to ? new Date(selectedDateRange.to) : null;
    if (checkOutDate) checkOutDate.setHours(0, 0, 0, 0);
    
    const availableDays = data.days.filter((day) => {
      if (!day.available) return false;
      // Exclude check-out date
      if (checkOutDate) {
        const dayDate = typeof day.date === 'string' ? new Date(day.date) : day.date;
        dayDate.setHours(0, 0, 0, 0);
        if (isSameDay(dayDate, checkOutDate)) return false;
      }
      return true;
    });
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
    
    // For single day bookings, nights = 0 (or 1 depending on your logic)
    const endDate = selectedDateRange.to || selectedDateRange.from;
    const nights = calculateNights(selectedDateRange.from, endDate);
    let addOnsTotal = 0;

    selectedAddOns.forEach((selectedAddOn) => {
      const addOn = availableAddOns.find((a: AddOn) => a.id === selectedAddOn.addOnId);
      if (addOn) {
        addOnsTotal += calculateAddOnPrice(
          addOn.price,
          addOn.type,
          selectedAddOn.quantity,
          nights,
          1 // TODO: Get actual guest count from form
        );
      }
    });

    // Return: base - discount + events + addons
    return totalAfterDiscount + addOnsTotal + eventsTotal;
  }, [data, selectedDateRange, selectedAddOns, availableAddOns, selectedOfferDetails, selectedEventDetails]);

  const containerStyle: React.CSSProperties = {
    padding: '24px',
    border: '1px solid #e5e7eb',
    borderRadius: '12px',
    backgroundColor: '#ffffff',
    margin: '24px 0',
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
    letterSpacing: '-0.01em'
  };

  if (!request || !selectedDateRange.from) {
    return (
      <div style={containerStyle} data-testid={TEST_IDS.availabilityPanel}>
        <p style={{ color: '#6b7280', fontSize: '14px', fontWeight: '300' }}>
          Please select a date to check availability
        </p>
      </div>
    );
  }

  if (loading) {
    return (
      <div style={containerStyle} data-testid={TEST_IDS.availabilityPanel}>
        <Spinner />
        <p style={{ marginTop: '12px', color: '#6b7280', fontSize: '14px', fontWeight: '300' }}>
          Checking availability...
        </p>
      </div>
    );
  }

  if (error) {
    return (
      <div style={containerStyle} data-testid={TEST_IDS.availabilityPanel}>
        <p style={{ color: '#dc2626', fontSize: '14px', fontWeight: '400' }}>
          Error: {error}
        </p>
      </div>
    );
  }

  if (!data) {
    return null;
  }

  // Exclude check-out date from pricing (you don't pay for the day you check out)
  const checkOutDate = selectedDateRange.to ? new Date(selectedDateRange.to) : null;
  if (checkOutDate) checkOutDate.setHours(0, 0, 0, 0);
  
  const availableDays = data.days.filter((day) => {
    if (!day.available) return false;
    // Exclude check-out date
    if (checkOutDate) {
      const dayDate = typeof day.date === 'string' ? new Date(day.date) : day.date;
      dayDate.setHours(0, 0, 0, 0);
      if (isSameDay(dayDate, checkOutDate)) return false;
    }
    return true;
  });
  const baseTotal = calculateTotal(availableDays);
  const endDate = selectedDateRange.to || selectedDateRange.from;
  const nights = calculateNights(selectedDateRange.from, endDate);

  return (
    <div style={containerStyle} data-testid={TEST_IDS.availabilityPanel}>
      {/* Room Image and Details Section */}
      {productDetails?.room && (
        <div style={{ 
          marginBottom: '24px', 
          paddingBottom: '24px', 
          borderBottom: '1px solid #e5e7eb' 
        }}>
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: 'minmax(0, 200px) 1fr', 
            gap: '20px',
            alignItems: 'start'
          }}
          className="availability-room-details-grid"
          >
            <style>{`
              @media (max-width: 768px) {
                .availability-room-details-grid {
                  grid-template-columns: 1fr !important;
                }
              }
            `}</style>
            {/* Room Image */}
            {productDetails.room.heroImage && (
              <div style={{
                width: '100%',
                maxWidth: '200px',
                aspectRatio: '4/3',
                borderRadius: '8px',
                overflow: 'hidden',
                backgroundColor: '#f3f4f6',
                flexShrink: 0
              }}
              className="availability-room-image"
              >
                <style>{`
                  @media (max-width: 768px) {
                    .availability-room-image {
                      max-width: 100% !important;
                      aspect-ratio: 16/9 !important;
                    }
                  }
                `}</style>
                <img 
                  src={productDetails.room.heroImage} 
                  alt={productDetails.room.name}
                  style={{
                    width: '100%',
                    height: '100%',
                    objectFit: 'cover'
                  }}
                />
              </div>
            )}
            
            {/* Room and Hotel Details */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              {/* Room Name */}
              <h3 style={{
                fontSize: '22px',
                fontWeight: '500',
                color: '#111827',
                fontFamily: "'Playfair Display', serif",
                letterSpacing: '-0.02em',
                margin: 0
              }}>
                {productDetails.room.name}
              </h3>
              
              {/* Hotel Name */}
              {productDetails.hotel && (
                <p style={{
                  fontSize: '14px',
                  color: '#6b7280',
                  fontWeight: '300',
                  margin: 0
                }}>
                  {productDetails.hotel.name}
                  {productDetails.hotel.city && ` • ${productDetails.hotel.city}`}
                  {productDetails.hotel.country && `, ${productDetails.hotel.country}`}
                </p>
              )}
              
              {/* Selected Dates */}
              <div style={{
                fontSize: '13px',
                color: '#374151',
                fontWeight: '300',
                marginTop: '4px'
              }}>
                <span style={{ fontWeight: '400' }}>Check-in:</span> {formatDate(selectedDateRange.from)}
                {selectedDateRange.to && (
                  <>
                    <span style={{ margin: '0 8px', color: '#d1d5db' }}>•</span>
                    <span style={{ fontWeight: '400' }}>Check-out:</span> {formatDate(selectedDateRange.to)}
                  </>
                )}
                {nights > 0 && (
                  <span style={{ marginLeft: '8px', color: '#6b7280' }}>
                    ({nights} night{nights !== 1 ? 's' : ''})
                  </span>
                )}
              </div>
              
              {/* Room Features (if available) */}
              {productDetails.room.features && 
               Array.isArray(productDetails.room.features) && 
               productDetails.room.features.length > 0 && (
                <div style={{
                  display: 'flex',
                  flexWrap: 'wrap',
                  gap: '8px',
                  marginTop: '8px'
                }}>
                  {productDetails.room.features.slice(0, 3).map((feature: string, idx: number) => (
                    <span key={idx} style={{
                      fontSize: '11px',
                      color: '#6b7280',
                      backgroundColor: '#f3f4f6',
                      padding: '4px 8px',
                      borderRadius: '4px',
                      fontWeight: '300'
                    }}>
                      {feature}
                    </span>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
      
      {/* Available Offers */}
      {offers.length > 0 && (
        <div style={{
          marginBottom: '24px',
          padding: '20px',
          backgroundColor: '#ffffff',
          border: '1px solid #e5e7eb',
          borderRadius: '8px'
        }}>
          <h4 style={{
            fontSize: '20px',
            fontWeight: '500',
            marginBottom: '16px',
            color: '#111827',
            fontFamily: "'Playfair Display', serif",
            letterSpacing: '-0.02em'
          }}>
            🎁 Special Offers
          </h4>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {offers.map((offer) => {
              const isSelected = selectedOffer?.offerId === offer.id;
              
              // Calculate number of nights
              const nights = selectedDateRange.from && selectedDateRange.to 
                ? Math.ceil((selectedDateRange.to.getTime() - selectedDateRange.from.getTime()) / (1000 * 60 * 60 * 24))
                : selectedDateRange.from ? 0 : 0;
              
              // Calculate days in advance
              const today = new Date();
              today.setHours(0, 0, 0, 0);
              const checkInDate = selectedDateRange.from ? new Date(selectedDateRange.from) : null;
              if (checkInDate) checkInDate.setHours(0, 0, 0, 0);
              const daysInAdvance = checkInDate ? Math.ceil((checkInDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24)) : 0;
              
              // Check date validity
              const isDateValid = offer.validFrom && offer.validTo 
                ? (new Date(offer.validFrom) <= (selectedDateRange.from || new Date()) && 
                   new Date(offer.validTo) >= (selectedDateRange.to || selectedDateRange.from || new Date()))
                : true;
              
              // Check minimum nights requirement
              const meetsMinNights = offer.minNights ? nights >= offer.minNights : true;
              
              // Check advance booking requirement
              const meetsAdvanceBooking = offer.minAdvanceBookingDays ? daysInAdvance >= offer.minAdvanceBookingDays : true;
              
              // Check weekend requirement for Weekend Getaway
              const isWeekendOffer = offer.name.toLowerCase().includes('weekend');
              let includesWeekend = true;
              if (isWeekendOffer && selectedDateRange.from && selectedDateRange.to) {
                const from = new Date(selectedDateRange.from);
                const to = new Date(selectedDateRange.to);
                includesWeekend = false;
                for (let d = new Date(from); d <= to; d.setDate(d.getDate() + 1)) {
                  if (d.getDay() === 0 || d.getDay() === 6) {
                    includesWeekend = true;
                    break;
                  }
                }
              }
              
              const isValid = isDateValid && meetsMinNights && meetsAdvanceBooking && includesWeekend;
              
              // Build validation message
              let validationMessage = '';
              if (!isValid) {
                if (!isDateValid) validationMessage = 'Not valid for selected dates';
                else if (!meetsMinNights) validationMessage = `Requires ${offer.minNights}+ nights`;
                else if (!meetsAdvanceBooking) validationMessage = `Book ${offer.minAdvanceBookingDays}+ days in advance`;
                else if (!includesWeekend) validationMessage = 'Must include a weekend (Sat/Sun)';
              }
              
              return (
              <div key={offer.id} style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                padding: '16px',
                backgroundColor: isSelected ? '#f9fafb' : '#ffffff',
                borderRadius: '6px',
                border: `1px solid ${isSelected ? '#111827' : '#e5e7eb'}`,
                opacity: isValid ? 1 : 0.5
              }}>
                <div style={{ flex: 1 }}>
                  <h5 style={{
                    fontSize: '15px',
                    fontWeight: '500',
                    color: '#111827',
                    marginBottom: '6px',
                    fontFamily: "'Playfair Display', serif"
                  }}>
                    {offer.name}
                    {isSelected && (
                      <span style={{
                        fontSize: '11px',
                        color: '#111827',
                        marginLeft: '8px',
                        fontWeight: '300',
                        textTransform: 'uppercase',
                        letterSpacing: '0.05em'
                      }}>
                        ✓ Applied
                      </span>
                    )}
                  </h5>
                  {offer.description && (
                    <p style={{
                      fontSize: '13px',
                      color: '#6b7280',
                      margin: '0 0 6px 0',
                      fontWeight: '300',
                      lineHeight: '1.5'
                    }}>
                      {offer.description}
                    </p>
                  )}
                  {offer.validFrom && offer.validTo && (
                    <p style={{
                      fontSize: '11px',
                      color: '#9ca3af',
                      margin: 0,
                      fontWeight: '300',
                      textTransform: 'uppercase',
                      letterSpacing: '0.05em'
                    }}>
                      Valid: {formatDate(new Date(offer.validFrom))} - {formatDate(new Date(offer.validTo))}
                    </p>
                  )}
                  {!isValid && validationMessage && (
                    <p style={{
                      fontSize: '11px',
                      color: '#dc2626',
                      margin: '6px 0 0 0',
                      fontWeight: '400'
                    }}>
                      {validationMessage}
                    </p>
                  )}
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: '10px' }}>
                  {offer.discount && (
                    <span style={{
                      fontSize: '20px',
                      fontWeight: '500',
                      color: '#111827',
                      fontFamily: "'Playfair Display', serif"
                    }}>
                      {offer.discount}% OFF
                    </span>
                  )}
                  <button
                    onClick={() => setSelectedOffer(isSelected ? null : { offerId: offer.id })}
                    disabled={!isValid}
                    style={{
                      padding: '8px 16px',
                      fontSize: '11px',
                      fontWeight: '300',
                      backgroundColor: !isValid ? '#e5e7eb' : (isSelected ? '#ffffff' : '#111827'),
                      color: !isValid ? '#9ca3af' : (isSelected ? '#111827' : '#ffffff'),
                      border: isSelected ? '1px solid #111827' : 'none',
                      borderRadius: '4px',
                      cursor: !isValid ? 'not-allowed' : 'pointer',
                      textTransform: 'uppercase',
                      letterSpacing: '0.1em',
                      transition: 'all 0.2s'
                    }}
                  >
                    {isSelected ? 'Remove' : 'Apply Offer'}
                  </button>
                </div>
              </div>
              );
            })}
          </div>
        </div>
      )}
      
      {/* Events on Selected Dates */}
      {eventsOnSelectedDates.length > 0 && (
        <div style={{
          marginBottom: '24px',
          padding: '20px',
          backgroundColor: '#ffffff',
          border: '1px solid #e5e7eb',
          borderRadius: '8px'
        }}>
          <h4 style={{
            fontSize: '20px',
            fontWeight: '500',
            marginBottom: '16px',
            color: '#111827',
            fontFamily: "'Playfair Display', serif",
            letterSpacing: '-0.02em'
          }}>
            🎉 Events During Your Stay
          </h4>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {eventsOnSelectedDates.map((event) => {
              const isSelected = selectedEvents.some((se: { eventId: string }) => se.eventId === event.id);
              return (
              <div key={event.id} style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                padding: '16px',
                backgroundColor: isSelected ? '#f9fafb' : '#ffffff',
                borderRadius: '6px',
                border: `1px solid ${isSelected ? '#111827' : '#e5e7eb'}`
              }}>
                <div style={{ flex: 1 }}>
                  <h5 style={{
                    fontSize: '15px',
                    fontWeight: '500',
                    color: '#111827',
                    marginBottom: '6px',
                    fontFamily: "'Playfair Display', serif"
                  }}>
                    {event.name}
                    {isSelected && (
                      <span style={{
                        fontSize: '11px',
                        color: '#111827',
                        marginLeft: '8px',
                        fontWeight: '300',
                        textTransform: 'uppercase',
                        letterSpacing: '0.05em'
                      }}>
                        ✓ Added
                      </span>
                    )}
                  </h5>
                  {event.eventDate && (
                    <p style={{
                      fontSize: '13px',
                      color: '#6b7280',
                      margin: '0 0 4px 0',
                      fontWeight: '300'
                    }}>
                      {formatDate(new Date(event.eventDate))}
                    </p>
                  )}
                  {event.location && (
                    <p style={{
                      fontSize: '11px',
                      color: '#9ca3af',
                      margin: 0,
                      fontWeight: '300',
                      textTransform: 'uppercase',
                      letterSpacing: '0.05em'
                    }}>
                      📍 {event.location}
                    </p>
                  )}
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: '10px' }}>
                  {event.price && (
                    <span style={{
                      fontSize: '18px',
                      fontWeight: '500',
                      color: '#111827',
                      fontFamily: "'Playfair Display', serif"
                    }}>
                      {formatPrice(event.price, 'GBP')}
                    </span>
                  )}
                  <button
                    onClick={() => toggleEvent(event.id)}
                    style={{
                      padding: '8px 16px',
                      fontSize: '11px',
                      fontWeight: '300',
                      backgroundColor: isSelected ? '#ffffff' : '#111827',
                      color: isSelected ? '#111827' : '#ffffff',
                      border: isSelected ? '1px solid #111827' : 'none',
                      borderRadius: '4px',
                      cursor: 'pointer',
                      textTransform: 'uppercase',
                      letterSpacing: '0.1em',
                      transition: 'all 0.2s'
                    }}
                  >
                    {isSelected ? 'Remove' : 'Add Event'}
                  </button>
                </div>
              </div>
              );
            })}
          </div>
        </div>
      )}
      
      <h3 style={{
        fontSize: '20px',
        fontWeight: '500',
        marginBottom: '20px',
        color: '#111827',
        fontFamily: "'Playfair Display', serif",
        letterSpacing: '-0.02em'
      }}>
        Pricing Summary
      </h3>
      {availableDays.length > 0 ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <p style={{ 
            marginBottom: '8px', 
            color: '#374151', 
            fontSize: '14px',
            fontWeight: '300'
          }}>
            {availableDays.length} day{availableDays.length !== 1 ? 's' : ''} available
          </p>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ color: '#6b7280', fontSize: '14px', fontWeight: '300' }}>Base price:</span>
              <span style={{ color: '#111827', fontSize: '14px', fontWeight: '500' }}>
                {formatPrice(baseTotal, data.currency)}
              </span>
            </div>
            {selectedOfferDetails && selectedOfferDetails.discount && (
              <div style={{ 
                display: 'flex', 
                justifyContent: 'space-between', 
                alignItems: 'center',
                fontSize: '13px',
                color: '#111827',
                fontWeight: '400',
                backgroundColor: '#f9fafb',
                padding: '10px 12px',
                borderRadius: '6px',
                marginTop: '8px'
              }}>
                <span>🎁 {selectedOfferDetails.name} ({selectedOfferDetails.discount}% off)</span>
                <span style={{ color: '#dc2626' }}>-{formatPrice((baseTotal * selectedOfferDetails.discount) / 100, data.currency)}</span>
              </div>
            )}
            {selectedEventDetails.length > 0 && (
              <div style={{ 
                display: 'flex', 
                flexDirection: 'column',
                gap: '8px',
                fontSize: '13px',
                color: '#6b7280',
                fontWeight: '300',
                marginTop: '8px'
              }}>
                {selectedEventDetails.map((event) => (
                  <div key={event.id} style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    padding: '8px 10px',
                    backgroundColor: '#f9fafb',
                    borderRadius: '4px',
                    border: '1px solid #e5e7eb'
                  }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <span style={{ color: '#111827', fontWeight: '400' }}>🎉 {event.name}</span>
                      <button
                        onClick={() => toggleEvent(event.id)}
                        style={{
                          padding: '2px 8px',
                          fontSize: '10px',
                          fontWeight: '300',
                          backgroundColor: 'transparent',
                          color: '#dc2626',
                          border: '1px solid #dc2626',
                          borderRadius: '3px',
                          cursor: 'pointer',
                          textTransform: 'uppercase',
                          letterSpacing: '0.05em'
                        }}
                      >
                        Remove
                      </button>
                    </div>
                    {event.price && (
                      <span style={{ fontWeight: '500', color: '#111827' }}>{formatPrice(event.price, data.currency)}</span>
                    )}
                  </div>
                ))}
              </div>
            )}
            {(selectedAddOns.length > 0 || selectedEventDetails.length > 0) && totalWithAddOns !== null && (
              <>
                {selectedAddOns.length > 0 && (
                  <div style={{ 
                    display: 'flex', 
                    justifyContent: 'space-between', 
                    alignItems: 'center',
                    fontSize: '13px',
                    color: '#6b7280',
                    fontWeight: '300'
                  }}>
                    <span>Add-ons:</span>
                    <span>{formatPrice(totalWithAddOns - baseTotal - (selectedEventDetails.reduce((sum, e) => sum + (e.price || 0), 0)), data.currency)}</span>
                  </div>
                )}
                <div style={{ 
                  display: 'flex', 
                  justifyContent: 'space-between', 
                  alignItems: 'center',
                  paddingTop: '16px',
                  borderTop: '1px solid #e5e7eb',
                  fontSize: '20px',
                  fontWeight: '500',
                  color: '#111827'
                }}>
                  <span>Total:</span>
                  <span>{formatPrice(totalWithAddOns, data.currency)}</span>
                </div>
              </>
            )}
            {selectedAddOns.length === 0 && selectedEventDetails.length === 0 && (
              <div style={{ 
                display: 'flex', 
                justifyContent: 'space-between', 
                alignItems: 'center',
                paddingTop: '16px',
                borderTop: '1px solid #e5e7eb',
                fontSize: '20px',
                fontWeight: '500',
                color: '#111827'
              }}>
                <span>Total:</span>
                <span>{formatPrice(totalWithAddOns !== null ? totalWithAddOns : baseTotal, data.currency)}</span>
              </div>
            )}
          </div>
        </div>
      ) : (
        <p style={{ color: '#6b7280', fontSize: '14px', fontWeight: '300' }}>
          No availability for selected dates
        </p>
      )}
    </div>
  );
};

