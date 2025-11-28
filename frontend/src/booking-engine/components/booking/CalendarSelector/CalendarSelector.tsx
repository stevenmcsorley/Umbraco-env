import { useBookingStore } from '../../../app/state/bookingStore';
import { TEST_IDS } from '../../../constants/testids';
import { formatDate, isSameDay } from '../../../utils/dateUtils';
import type { CalendarDay } from '../../../types/domain.types';

export interface CalendarSelectorProps {
  availabilityDays?: CalendarDay[];
  className?: string;
}

export const CalendarSelector = ({ availabilityDays = [], className = '' }: CalendarSelectorProps) => {
  const { selectedDateRange, setSelectedDateRange } = useBookingStore();
  const today = new Date();
  const startDate = new Date(today);
  startDate.setMonth(startDate.getMonth() - 1);
  const endDate = new Date(today);
  endDate.setMonth(endDate.getMonth() + 2);

  const handleDateClick = (date: Date) => {
    if (!selectedDateRange.from || (selectedDateRange.from && selectedDateRange.to)) {
      setSelectedDateRange(date, null);
    } else if (selectedDateRange.from && !selectedDateRange.to) {
      if (date < selectedDateRange.from) {
        setSelectedDateRange(date, null);
      } else {
        setSelectedDateRange(selectedDateRange.from, date);
      }
    }
  };

  const getAvailabilityForDate = (date: Date): CalendarDay | undefined => {
    return availabilityDays.find(day => {
      const dayDate = typeof day.date === 'string' ? new Date(day.date) : day.date;
      return isSameDay(dayDate, date);
    });
  };

  const renderCalendar = () => {
    const weeks: Date[][] = [];
    let currentWeek: Date[] = [];
    const current = new Date(startDate);

    while (current <= endDate) {
      if (currentWeek.length === 7) {
        weeks.push(currentWeek);
        currentWeek = [];
      }
      currentWeek.push(new Date(current));
      current.setDate(current.getDate() + 1);
    }
    if (currentWeek.length > 0) {
      weeks.push(currentWeek);
    }

    return weeks.map((week, weekIndex) => (
      <div key={weekIndex} className="grid grid-cols-7 gap-1">
        {week.map((date) => {
          const availability = getAvailabilityForDate(date);
          const isSelected = 
            (selectedDateRange.from && isSameDay(selectedDateRange.from, date)) ||
            (selectedDateRange.to && isSameDay(selectedDateRange.to, date));
          const isInRange = 
            selectedDateRange.from && selectedDateRange.to &&
            date >= selectedDateRange.from && date <= selectedDateRange.to;
          const isPast = date < today;
          const isAvailable = availability?.available ?? false;

          return (
            <button
              key={formatDate(date)}
              onClick={() => !isPast && handleDateClick(date)}
              disabled={isPast || !isAvailable}
              className={`
                p-2 rounded text-sm
                ${isPast ? 'opacity-30 cursor-not-allowed' : ''}
                ${isSelected ? 'bg-blue-600 text-white' : ''}
                ${isInRange && !isSelected ? 'bg-blue-100' : ''}
                ${isAvailable && !isPast ? 'hover:bg-blue-200 cursor-pointer' : ''}
                ${!isAvailable && !isPast ? 'opacity-50 cursor-not-allowed' : ''}
              `}
              data-testid={TEST_IDS.calendarDay(formatDate(date))}
            >
              {date.getDate()}
              {availability?.price && (
                <div className="text-xs mt-1">£{availability.price}</div>
              )}
            </button>
          );
        })}
      </div>
    ));
  };

  return (
    <div className={className} data-testid={TEST_IDS.calendarGrid}>
      <div className="mb-4">
        <h3 className="text-lg font-semibold mb-2">Select Dates</h3>
        {selectedDateRange.from && (
          <p className="text-sm text-gray-600">
            From: {formatDate(selectedDateRange.from)}
            {selectedDateRange.to && ` To: ${formatDate(selectedDateRange.to)}`}
          </p>
        )}
      </div>
      {renderCalendar()}
    </div>
  );
};

