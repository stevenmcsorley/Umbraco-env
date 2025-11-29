import { useState, useMemo, useEffect } from 'react';
import { useBookingStore } from '../../../app/state/bookingStore';
import { TEST_IDS } from '../../../constants/testids';
import { formatDate, isSameDay } from '../../../utils/dateUtils';
import { useAvailability } from '../../../hooks/useAvailability';
import { useEvents } from '../../../hooks/useEvents';
import type { CalendarDay } from '../../../types/domain.types';

export interface CalendarSelectorProps {
  availabilityDays?: CalendarDay[];
  className?: string;
  hotelId?: string;
}

export const CalendarSelector = ({ availabilityDays = [], className = '', hotelId }: CalendarSelectorProps) => {
  const { selectedProductId, selectedDateRange, setSelectedDateRange, availabilityResult, setAvailabilityResult } = useBookingStore();
  
  // Fetch events for this hotel to display on calendar
  const { events } = useEvents(hotelId);
  
  // Memoize today to prevent recreating on every render
  const today = useMemo(() => {
    const date = new Date();
    date.setHours(0, 0, 0, 0);
    return date;
  }, []);
  
  // Start calendar from today's month
  const [currentMonth, setCurrentMonth] = useState(() => {
    const month = new Date(today);
    month.setDate(1);
    return month;
  });
  
  // Track if we've already fetched initial availability to prevent loops
  const [hasFetchedInitial, setHasFetchedInitial] = useState(false);
  const [lastProductId, setLastProductId] = useState<string | null>(null);
  
  // Memoize the end date to prevent re-creating it on every render
  const endDate90Days = useMemo(() => {
    return new Date(today.getTime() + 90 * 24 * 60 * 60 * 1000);
  }, [today]);
  
  // Reset fetch flag when productId changes
  useEffect(() => {
    if (selectedProductId !== lastProductId) {
      setHasFetchedInitial(false);
      setLastProductId(selectedProductId);
    }
  }, [selectedProductId, lastProductId]);
  
  // Fetch availability for a wide range when productId is set (to show prices before date selection)
  // Only fetch once when productId is set and we haven't fetched yet
  const availabilityRequest = useMemo(() => {
    if (selectedProductId && !hasFetchedInitial && !availabilityResult) {
      return {
        productId: selectedProductId,
        from: today,
        to: endDate90Days
      };
    }
    return null;
  }, [selectedProductId, hasFetchedInitial, availabilityResult, today, endDate90Days]);
  
  const { data: initialAvailability } = useAvailability(availabilityRequest);
  
  // Store initial availability data so calendar can show prices (only once per productId)
  useEffect(() => {
    if (initialAvailability && selectedProductId && !hasFetchedInitial) {
      setAvailabilityResult(initialAvailability);
      setHasFetchedInitial(true);
    }
  }, [initialAvailability, selectedProductId, hasFetchedInitial, setAvailabilityResult]);
  
  // Use availability from store if available, otherwise use prop
  const calendarAvailabilityDays = availabilityResult?.days || availabilityDays;
  
  // Find the earliest available date (or use today)
  const earliestAvailableDate = useMemo(() => {
    if (calendarAvailabilityDays.length > 0) {
      const availableDates = calendarAvailabilityDays
        .filter(day => day.available)
        .map(day => {
          const dayDate = typeof day.date === 'string' ? new Date(day.date) : day.date;
          dayDate.setHours(0, 0, 0, 0);
          return dayDate;
        })
        .filter(date => date >= today)
        .sort((a, b) => a.getTime() - b.getTime());
      
      if (availableDates.length > 0) {
        return availableDates[0];
      }
    }
    return today;
  }, [calendarAvailabilityDays]);
  
  // Calculate visible date range starting from current month (no past months)
  const startDate = useMemo(() => {
    const start = new Date(currentMonth);
    start.setDate(1);
    // Set to first day of week (Sunday), but don't go before today
    const dayOfWeek = start.getDay();
    const firstDay = new Date(start);
    firstDay.setDate(start.getDate() - dayOfWeek);
    
    // If first day is before today, start from today's week
    if (firstDay < today) {
      const todayWeekStart = new Date(today);
      todayWeekStart.setDate(today.getDate() - today.getDay());
      return todayWeekStart;
    }
    
    return firstDay;
  }, [currentMonth, today]);
  
  const endDate = useMemo(() => {
    const end = new Date(currentMonth);
    end.setDate(1);
    end.setMonth(end.getMonth() + 2); // Show 3 months total
    // Get last day of that month
    end.setDate(0);
    // Set to last day of week (Saturday)
    const dayOfWeek = end.getDay();
    end.setDate(end.getDate() + (6 - dayOfWeek));
    return end;
  }, [currentMonth]);
  
  // Prevent navigating to past months
  const navigateMonth = (direction: 'prev' | 'next') => {
    setCurrentMonth(prev => {
      const newMonth = new Date(prev);
      newMonth.setMonth(newMonth.getMonth() + (direction === 'next' ? 1 : -1));
      
      // Don't allow going before current month
      const currentMonthStart = new Date(today);
      currentMonthStart.setDate(1);
      currentMonthStart.setHours(0, 0, 0, 0);
      
      const newMonthStart = new Date(newMonth);
      newMonthStart.setDate(1);
      newMonthStart.setHours(0, 0, 0, 0);
      
      if (newMonthStart < currentMonthStart) {
        return prev; // Don't change if trying to go to past
      }
      
      return newMonth;
    });
  };

  const handleDateClick = (date: Date) => {
    const normalizedDate = new Date(date);
    normalizedDate.setHours(0, 0, 0, 0);
    
    const fromDate = selectedDateRange.from ? new Date(selectedDateRange.from) : null;
    const toDate = selectedDateRange.to ? new Date(selectedDateRange.to) : null;
    
    if (fromDate) fromDate.setHours(0, 0, 0, 0);
    
    // If no date selected yet, select this date as single day
    if (!fromDate) {
      setSelectedDateRange(normalizedDate, null);
      return;
    }
    
    // If same date clicked, do nothing (keep single day selection)
    if (fromDate && isSameDay(fromDate, normalizedDate)) {
      return;
    }
    
    // If we have a single day selected (no to date)
    if (fromDate && !toDate) {
      // Clicking a different day creates a range
      if (normalizedDate < fromDate) {
        // Selected date is before start date, make it the new start (single day)
        setSelectedDateRange(normalizedDate, null);
      } else {
        // Create range from existing date to clicked date
        setSelectedDateRange(fromDate, normalizedDate);
      }
      return;
    }
    
    // If we have a range selected (both from and to)
    if (fromDate && toDate) {
      // Clicking any date resets to single day selection
      setSelectedDateRange(normalizedDate, null);
      return;
    }
  };

  const getAvailabilityForDate = (date: Date): CalendarDay | undefined => {
    return calendarAvailabilityDays.find(day => {
      const dayDate = typeof day.date === 'string' ? new Date(day.date) : day.date;
      return isSameDay(dayDate, date);
    });
  };
  
  // Get events for a specific date
  const getEventsForDate = (date: Date) => {
    return events.filter(event => {
      if (!event.eventDate) return false;
      const eventDate = new Date(event.eventDate);
      eventDate.setHours(0, 0, 0, 0);
      return isSameDay(eventDate, date);
    });
  };

  const calendarWeeks = useMemo(() => {
    const weeks: Date[][] = [];
    let currentWeek: Date[] = [];
    const current = new Date(startDate);

    while (current <= endDate) {
      if (currentWeek.length === 7) {
        weeks.push([...currentWeek]);
        currentWeek = [];
      }
      currentWeek.push(new Date(current));
      current.setDate(current.getDate() + 1);
    }
    if (currentWeek.length > 0) {
      // Pad last week if needed
      while (currentWeek.length < 7) {
        const lastDate = new Date(currentWeek[currentWeek.length - 1]);
        lastDate.setDate(lastDate.getDate() + 1);
        currentWeek.push(lastDate);
      }
      weeks.push(currentWeek);
    }
    return weeks;
  }, [startDate, endDate]);

  const renderedWeeks = calendarWeeks.map((week, weekIndex) => {
    return (
      <div 
        key={weekIndex} 
        style={{ 
          display: 'grid', 
          gridTemplateColumns: 'repeat(7, minmax(0, 1fr))', 
          gap: '6px',
          marginBottom: '10px',
          width: '100%',
          maxWidth: '100%'
        }}
      >
              {week.map((date, dateIndex) => {
          const normalizedDate = new Date(date);
          normalizedDate.setHours(0, 0, 0, 0);
          
          const availability = getAvailabilityForDate(normalizedDate);
          const dateEvents = getEventsForDate(normalizedDate);
          const fromDate = selectedDateRange.from ? new Date(selectedDateRange.from) : null;
          const toDate = selectedDateRange.to ? new Date(selectedDateRange.to) : null;
          
          if (fromDate) fromDate.setHours(0, 0, 0, 0);
          if (toDate) toDate.setHours(0, 0, 0, 0);
          
          const isSelected = 
            (fromDate && isSameDay(fromDate, normalizedDate)) ||
            (toDate && isSameDay(toDate, normalizedDate));
          const isInRange = 
            fromDate && toDate &&
            normalizedDate >= fromDate && normalizedDate <= toDate;
          const isPast = normalizedDate < today;
          // Show availability based on data, or assume available if future date and no data yet
          const isAvailable = availability ? availability.available : (normalizedDate >= today && !isPast);
          
          // Hide dates from previous months (show as empty cells)
          const isFromPreviousMonth = normalizedDate < today && normalizedDate.getMonth() !== currentMonth.getMonth();

          const buttonStyle: React.CSSProperties = {
            padding: '10px 6px',
            borderRadius: '6px',
            fontSize: '14px',
            fontWeight: '400',
            border: isSelected ? '1px solid #111827' : '1px solid #e5e7eb',
            backgroundColor: isSelected 
              ? '#111827' // gray-900 for selected
              : isInRange 
                ? '#f3f4f6' // gray-100 for range
                : '#ffffff', // white for normal
            color: isSelected 
              ? '#ffffff' 
              : isPast 
                ? '#9ca3af' // gray-400 for past
                : '#111827', // gray-900 for normal
            cursor: (isPast || !isAvailable) ? 'not-allowed' : 'pointer',
            opacity: isPast ? 0.5 : (!isAvailable ? 0.7 : 1),
            height: '56px',
            width: '100%',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            transition: 'all 0.15s ease',
            boxSizing: 'border-box',
            fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
            letterSpacing: '-0.01em'
          };

          // Hide past dates from previous months
          if (isFromPreviousMonth) {
            return <div key={`${weekIndex}-${dateIndex}-empty`} style={{ height: '56px' }} />;
          }
          
          return (
            <button
              key={`${weekIndex}-${dateIndex}-${formatDate(normalizedDate)}`}
              onClick={(e) => {
                e.preventDefault();
                if (!isPast && isAvailable) {
                  handleDateClick(normalizedDate);
                }
              }}
              disabled={isPast || !isAvailable}
              style={buttonStyle}
              data-testid={TEST_IDS.calendarDay(formatDate(normalizedDate))}
            >
              <span style={{ 
                fontWeight: '500', 
                color: isSelected ? '#ffffff' : (isPast ? '#9ca3af' : '#111827'),
                lineHeight: '1.2',
                fontSize: '15px'
              }}>
                {normalizedDate.getDate()}
              </span>
              {/* Show event indicator if there's an event on this date */}
              {dateEvents.length > 0 && (
                <span style={{ 
                  fontSize: '9px', 
                  marginTop: '2px', 
                  color: isSelected ? '#fbbf24' : '#f59e0b',
                  lineHeight: '1',
                  fontWeight: '600',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px'
                }}>
                  Event
                </span>
              )}
              {/* Always show price if available, even before date selection */}
              {availability?.price !== undefined && !dateEvents.length && (
                <span style={{ 
                  fontSize: '11px', 
                  marginTop: '3px', 
                  color: isSelected ? '#d1d5db' : (isPast ? '#9ca3af' : '#6b7280'),
                  lineHeight: '1',
                  fontWeight: '300'
                }}>
                  £{availability.price}
                </span>
              )}
            </button>
          );
        })}
      </div>
    );
  });

  return (
    <div 
      className={`${className} calendar-selector`} 
      data-testid={TEST_IDS.calendarGrid} 
      style={{ 
        minHeight: '200px', 
        padding: '24px', 
        border: '1px solid #e5e7eb', 
        borderRadius: '12px', 
        backgroundColor: '#ffffff',
        margin: '24px 0',
        display: 'block',
        visibility: 'visible',
        width: '100%',
        maxWidth: '100%',
        boxSizing: 'border-box',
        fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
        letterSpacing: '-0.01em'
      }}
    >
      <div style={{ marginBottom: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '12px' }}>
        <div>
          <h3 style={{ 
            fontSize: '20px', 
            fontWeight: '500', 
            marginBottom: '10px', 
            color: '#111827',
            fontFamily: "'Playfair Display', serif",
            letterSpacing: '-0.02em'
          }}>
            Select Dates
          </h3>
          {selectedDateRange.from && (
            <p style={{ 
              fontSize: '14px', 
              color: '#6b7280', 
              marginTop: '6px',
              fontWeight: '300',
              letterSpacing: '0.01em'
            }}>
              From: {formatDate(selectedDateRange.from)}
              {selectedDateRange.to && ` To: ${formatDate(selectedDateRange.to)}`}
            </p>
          )}
        </div>
        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
          <button
            onClick={() => navigateMonth('prev')}
            disabled={(() => {
              const currentMonthStart = new Date(today);
              currentMonthStart.setDate(1);
              currentMonthStart.setHours(0, 0, 0, 0);
              const prevMonth = new Date(currentMonth);
              prevMonth.setMonth(prevMonth.getMonth() - 1);
              prevMonth.setDate(1);
              prevMonth.setHours(0, 0, 0, 0);
              return prevMonth < currentMonthStart;
            })()}
            style={{
              padding: '8px 14px',
              border: '1px solid #e5e7eb',
              borderRadius: '6px',
              backgroundColor: '#ffffff',
              color: '#374151',
              cursor: (() => {
                const currentMonthStart = new Date(today);
                currentMonthStart.setDate(1);
                currentMonthStart.setHours(0, 0, 0, 0);
                const prevMonth = new Date(currentMonth);
                prevMonth.setMonth(prevMonth.getMonth() - 1);
                prevMonth.setDate(1);
                prevMonth.setHours(0, 0, 0, 0);
                return prevMonth < currentMonthStart ? 'not-allowed' : 'pointer';
              })(),
              fontSize: '13px',
              fontWeight: '400',
              letterSpacing: '0.01em',
              transition: 'all 0.15s ease',
              opacity: (() => {
                const currentMonthStart = new Date(today);
                currentMonthStart.setDate(1);
                currentMonthStart.setHours(0, 0, 0, 0);
                const prevMonth = new Date(currentMonth);
                prevMonth.setMonth(prevMonth.getMonth() - 1);
                prevMonth.setDate(1);
                prevMonth.setHours(0, 0, 0, 0);
                return prevMonth < currentMonthStart ? 0.4 : 1;
              })()
            }}
            onMouseEnter={(e) => {
              if (!e.currentTarget.disabled) {
                e.currentTarget.style.backgroundColor = '#f9fafb';
                e.currentTarget.style.borderColor = '#d1d5db';
              }
            }}
            onMouseLeave={(e) => {
              if (!e.currentTarget.disabled) {
                e.currentTarget.style.backgroundColor = '#ffffff';
                e.currentTarget.style.borderColor = '#e5e7eb';
              }
            }}
          >
            ← Prev
          </button>
          <span style={{ 
            fontSize: '14px', 
            color: '#111827', 
            minWidth: '140px', 
            textAlign: 'center',
            fontWeight: '500',
            letterSpacing: '-0.01em'
          }}>
            {currentMonth.toLocaleDateString('en-US', { month: 'long', year: 'numeric' })}
          </span>
          <button
            onClick={() => navigateMonth('next')}
            style={{
              padding: '8px 14px',
              border: '1px solid #e5e7eb',
              borderRadius: '6px',
              backgroundColor: '#ffffff',
              color: '#374151',
              cursor: 'pointer',
              fontSize: '13px',
              fontWeight: '400',
              letterSpacing: '0.01em',
              transition: 'all 0.15s ease'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = '#f9fafb';
              e.currentTarget.style.borderColor = '#d1d5db';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = '#ffffff';
              e.currentTarget.style.borderColor = '#e5e7eb';
            }}
          >
            Next →
          </button>
        </div>
      </div>
      <div style={{ width: '100%', maxWidth: '100%' }}>
        {calendarWeeks && calendarWeeks.length > 0 ? (
          <>
            {/* Day headers */}
            <div style={{ 
              display: 'grid', 
              gridTemplateColumns: 'repeat(7, minmax(0, 1fr))', 
              gap: '6px', 
              marginBottom: '12px', 
              fontWeight: '400', 
              fontSize: '11px', 
              textAlign: 'center',
              color: '#6b7280',
              width: '100%',
              maxWidth: '100%',
              textTransform: 'uppercase',
              letterSpacing: '0.05em'
            }}>
              {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(day => (
                <div key={day} style={{ padding: '10px 4px', color: '#6b7280' }}>{day}</div>
              ))}
            </div>
            <div style={{ width: '100%', maxWidth: '100%' }}>
              {renderedWeeks}
            </div>
          </>
        ) : (
          <div style={{ 
            color: '#6b7280', 
            fontSize: '14px', 
            padding: '20px', 
            textAlign: 'center',
            fontWeight: '300',
            letterSpacing: '0.01em'
          }}>
            Loading calendar...
          </div>
        )}
      </div>
    </div>
  );
};

