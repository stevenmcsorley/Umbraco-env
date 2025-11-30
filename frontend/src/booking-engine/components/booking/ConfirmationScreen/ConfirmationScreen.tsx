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
    }
  }, [confirmation]);

  if (!confirmation) return null;

  const containerStyle: React.CSSProperties = {
    padding: '32px',
    backgroundColor: '#ffffff',
    borderRadius: '12px',
    boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif"
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
    <div className={className} style={containerStyle} data-testid={TEST_IDS.confirmationScreen}>
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

      <div style={sectionStyle}>
        <div style={labelStyle}>Booking Reference</div>
        <div style={{ ...valueStyle, fontFamily: 'monospace', fontWeight: '600' }} data-testid={TEST_IDS.confirmationReference}>
          {confirmation.bookingId}
        </div>
      </div>

      {(confirmation.productName || confirmation.hotelName) && (
        <div style={sectionStyle}>
          <div style={labelStyle}>Reservation Details</div>
          {confirmation.productName && (
            <div style={{ 
              ...valueStyle, 
              fontSize: '18px',
              fontWeight: '600', 
              marginBottom: '6px',
              fontFamily: "'Playfair Display', serif",
              letterSpacing: '-0.02em'
            }}>
              {confirmation.productName}
            </div>
          )}
          {confirmation.hotelName && (
            <div style={{ 
              ...valueStyle, 
              fontSize: '14px', 
              color: '#6b7280',
              fontWeight: '300',
              marginTop: '4px'
            }}>
              {confirmation.hotelName}
              {confirmation.hotelLocation && ` • ${confirmation.hotelLocation}`}
            </div>
          )}
        </div>
      )}

      <div style={sectionStyle}>
        <div style={labelStyle}>Dates</div>
        <div style={valueStyle}>
          {typeof confirmation.from === 'string' 
            ? new Date(confirmation.from).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
            : confirmation.from.toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}
          {' → '}
          {typeof confirmation.to === 'string' 
            ? new Date(confirmation.to).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
            : confirmation.to.toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })}
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

      {confirmation.totalPrice !== undefined && (
        <div style={totalStyle}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>Total Amount</span>
            <span>{formatPrice(confirmation.totalPrice, confirmation.currency || 'GBP')}</span>
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

