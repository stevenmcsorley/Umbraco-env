import { useState } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';
import { useBookingFlow } from '../../../hooks/useBookingFlow';
import { Input } from '../../ui/Input';
import { Button } from '../../ui/Button';
import { TEST_IDS } from '../../../constants/testids';
import { AnalyticsManager } from '../../../../services/analytics';

export interface BookingFormProps {
  className?: string;
  user?: {
    userId: string;
    email: string;
    firstName: string;
    lastName: string;
    phone?: string;
  } | null;
}

export const BookingForm = ({ className = '', user }: BookingFormProps) => {
  const { selectedProductId, selectedDateRange, selectedAddOns, selectedEvents, setConfirmation, setError } = useBookingStore();
  const { submitBooking, loading } = useBookingFlow();
  
  const [firstName, setFirstName] = useState(user?.firstName || '');
  const [lastName, setLastName] = useState(user?.lastName || '');
  const [email, setEmail] = useState(user?.email || '');
  const [phone, setPhone] = useState(user?.phone || '');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!selectedProductId || !selectedDateRange.from) {
      setError('Please select product and dates');
      return;
    }

    try {
      const booking = await submitBooking({
        productId: selectedProductId,
        from: selectedDateRange.from,
        to: selectedDateRange.to || selectedDateRange.from,
        guestDetails: {
          firstName,
          lastName,
          email,
          phone: phone || undefined
        },
        addOns: selectedAddOns.length > 0 ? selectedAddOns : undefined,
        events: selectedEvents.length > 0 ? selectedEvents : undefined
      });

      setConfirmation(booking);
      AnalyticsManager.trackBookingConfirmed(booking.bookingId);
    } catch (err: any) {
      setError(err.message);
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

