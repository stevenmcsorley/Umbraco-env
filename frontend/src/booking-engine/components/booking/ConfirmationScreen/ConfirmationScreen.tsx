import { useEffect } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';
import { TEST_IDS } from '../../../constants/testids';
import { formatPrice } from '../../../utils/priceUtils';

export interface ConfirmationScreenProps {
  className?: string;
}

export const ConfirmationScreen = ({ className = '' }: ConfirmationScreenProps) => {
  const { confirmation, reset } = useBookingStore();

  // Hide the "Reserve Your Stay" section when confirmation is shown
  useEffect(() => {
    if (confirmation) {
      const bookingSection = document.getElementById('booking');
      if (bookingSection) {
        const leftColumn = bookingSection.querySelector('.grid > div:first-child');
        if (leftColumn) {
          (leftColumn as HTMLElement).style.display = 'none';
        }
      }
      
      // Debug: log confirmation data
      console.log('[ConfirmationScreen] Confirmation data:', {
        productName: confirmation.productName,
        hotelName: confirmation.hotelName,
        hotelLocation: confirmation.hotelLocation,
        bookingId: confirmation.bookingId
      });
    }
  }, [confirmation]);

  if (!confirmation) return null;

  const containerStyle: React.CSSProperties = {
    padding: '32px',
    backgroundColor: '#ffffff',
    borderRadius: '12px',
    boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
    width: '100%',
    maxWidth: '100%'
  };

  const titleStyle: React.CSSProperties = {
    fontSize: '28px',
    fontWeight: '700',
    marginBottom: '24px',
    color: '#059669',
    letterSpacing: '-0.02em'
  };

  const sectionStyle: React.CSSProperties = {
    marginBottom: '20px',
    paddingBottom: '20px',
    borderBottom: '1px solid #e5e7eb'
  };

  const labelStyle: React.CSSProperties = {
    fontSize: '13px',
    fontWeight: '500',
    color: '#6b7280',
    textTransform: 'uppercase',
    letterSpacing: '0.05em',
    marginBottom: '4px'
  };

  const valueStyle: React.CSSProperties = {
    fontSize: '16px',
    color: '#111827',
    fontWeight: '400'
  };

  const totalStyle: React.CSSProperties = {
    fontSize: '24px',
    fontWeight: '700',
    color: '#111827',
    marginTop: '24px',
    paddingTop: '24px',
    borderTop: '2px solid #e5e7eb'
  };

  const buttonStyle: React.CSSProperties = {
    width: '100%',
    padding: '14px 24px',
    backgroundColor: '#111827',
    color: '#ffffff',
    border: 'none',
    borderRadius: '8px',
    fontSize: '16px',
    fontWeight: '500',
    cursor: 'pointer',
    marginTop: '24px',
    transition: 'background-color 0.2s',
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif"
  };

  return (
    <div className={className} style={{ ...containerStyle, margin: '40px auto' }} data-testid={TEST_IDS.confirmationScreen}>
      <div style={{ textAlign: 'center', marginBottom: '32px' }}>
        <div style={{ 
          width: '64px', 
          height: '64px', 
          borderRadius: '50%', 
          backgroundColor: '#d1fae5', 
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'center',
          margin: '0 auto 16px'
        }}>
          <span style={{ fontSize: '32px' }}>✓</span>
        </div>
        <h2 style={titleStyle}>Booking Confirmed!</h2>
        <p style={{ color: '#6b7280', fontSize: '14px' }}>
          Your reservation has been successfully created. A confirmation email has been sent to your inbox.
        </p>
      </div>

      {/* Reservation Details with Booking Reference and Price on the right */}
      <div style={{
        display: 'flex',
        flexDirection: 'row',
        gap: '20px',
        alignItems: 'flex-start',
        padding: '16px 0',
        borderBottom: '1px solid #e5e7eb',
        textAlign: 'left',
        marginBottom: '20px'
      }}
      className="confirmation-details-row"
      >
        <style>{`
          @media (max-width: 768px) {
            .confirmation-details-row {
              flex-direction: column !important;
            }
          }
        `}</style>
        {/* Left side: Reservation Details */}
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '12px', minWidth: 0 }}>
          <div style={labelStyle}>Reservation Details</div>
          {confirmation.productName ? (
            <div style={{ 
              fontSize: '22px',
              fontWeight: '500',
              color: '#111827',
              fontFamily: "'Playfair Display', serif",
              letterSpacing: '-0.02em',
              margin: 0
            }}>
              {confirmation.productName}
            </div>
          ) : (
            <div style={{ 
              fontSize: '22px',
              fontWeight: '500',
              color: '#111827',
              fontFamily: "'Playfair Display', serif",
              letterSpacing: '-0.02em',
              margin: 0
            }}>
              Room Booking
            </div>
          )}
          {confirmation.hotelName && (
            <div style={{ 
              fontSize: '14px', 
              color: '#6b7280',
              fontWeight: '300',
              margin: 0
            }}>
              {confirmation.hotelName}
              {confirmation.hotelLocation && ` • ${confirmation.hotelLocation}`}
            </div>
          )}
          <div style={{
            fontSize: '13px',
            color: '#374151',
            fontWeight: '300',
            marginTop: '4px'
          }}>
            <span style={{ fontWeight: '400' }}>Check-in:</span>{' '}
            {typeof confirmation.from === 'string' 
              ? new Date(confirmation.from).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
              : confirmation.from.toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}
            <span style={{ margin: '0 8px', color: '#d1d5db' }}>•</span>
            <span style={{ fontWeight: '400' }}>Check-out:</span>{' '}
            {typeof confirmation.to === 'string' 
              ? new Date(confirmation.to).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
              : confirmation.to.toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}
          </div>
        </div>

        {/* Right side: Booking Reference and Price */}
        <div style={{ 
          flex: '0 0 auto', 
          display: 'flex', 
          flexDirection: 'column', 
          gap: '12px',
          textAlign: 'right',
          alignItems: 'flex-end',
          minWidth: '200px'
        }}>
          <div>
            <div style={{ ...labelStyle, marginBottom: '4px' }}>Booking Reference</div>
            <div style={{ 
              ...valueStyle, 
              fontFamily: 'monospace', 
              fontWeight: '600',
              fontSize: '14px',
              wordBreak: 'break-all'
            }} data-testid={TEST_IDS.confirmationReference}>
              {confirmation.bookingId}
            </div>
          </div>
          {confirmation.totalPrice !== undefined && (
            <div>
              <div style={{ 
                fontSize: '18px',
                fontWeight: '600',
                color: '#111827',
                margin: '0 0 8px 0'
              }}>
                {formatPrice(confirmation.totalPrice, confirmation.currency || 'GBP')}
              </div>
              <span style={{
                display: 'inline-block',
                padding: '4px 12px',
                borderRadius: '4px',
                fontSize: '12px',
                fontWeight: '500',
                backgroundColor: '#dcfce7',
                color: '#166534'
              }}>
                Confirmed
              </span>
            </div>
          )}
        </div>
      </div>

      <div style={sectionStyle}>
        <div style={labelStyle}>Guest Information</div>
        <div style={valueStyle}>
          {confirmation.guestDetails.firstName} {confirmation.guestDetails.lastName}
        </div>
        <div style={{ ...valueStyle, fontSize: '14px', color: '#6b7280', marginTop: '4px' }}>
          {confirmation.guestDetails.email}
        </div>
        {confirmation.guestDetails.phone && (
          <div style={{ ...valueStyle, fontSize: '14px', color: '#6b7280', marginTop: '4px' }}>
            {confirmation.guestDetails.phone}
          </div>
        )}
      </div>

      {confirmation.addOns && confirmation.addOns.length > 0 && (
        <div style={sectionStyle}>
          <div style={labelStyle}>Add-ons</div>
          <div style={{ marginTop: '8px' }}>
            {confirmation.addOns.map((addOn) => (
              <div key={addOn.addOnId} style={{ 
                display: 'flex', 
                justifyContent: 'space-between', 
                marginBottom: '8px',
                fontSize: '14px'
              }}>
                <span>{addOn.name} (x{addOn.quantity})</span>
                <span style={{ fontWeight: '500' }}>
                  {formatPrice(addOn.price, confirmation.currency || 'GBP')}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}


      <button 
        onClick={reset} 
        style={buttonStyle}
        onMouseOver={(e) => e.currentTarget.style.backgroundColor = '#1f2937'}
        onMouseOut={(e) => e.currentTarget.style.backgroundColor = '#111827'}
      >
        Book Another Stay
      </button>
    </div>
  );
};

