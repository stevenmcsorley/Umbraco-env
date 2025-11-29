export const TEST_IDS = {
  // Product
  productCard: (id?: string) => `booking-product-card${id ? `-${id}` : ''}`,
  
  // Calendar
  calendarDay: (date: string) => `booking-calendar-day-${date}`,
  calendarGrid: 'booking-calendar-grid',
  
  // Availability
  availabilityPanel: 'booking-availability-panel',
  availabilityCheckButton: 'booking-availability-check-button',
  
  // Booking Form
  bookingForm: 'booking-booking-form',
  firstNameInput: 'booking-first-name-input',
  lastNameInput: 'booking-last-name-input',
  emailInput: 'booking-email-input',
  phoneInput: 'booking-phone-input',
  submitButton: 'booking-submit-button',
  
  // Confirmation
  confirmationScreen: 'booking-confirmation-screen',
  confirmationReference: 'booking-confirmation-reference',
  
  // Add-ons
  addOnsSelector: 'booking-addons-selector',
  addOnCard: (id: string) => `booking-addon-card-${id}`,
  addOnAdd: (id: string) => `booking-addon-add-${id}`,
  addOnIncrease: (id: string) => `booking-addon-increase-${id}`,
  addOnDecrease: (id: string) => `booking-addon-decrease-${id}`,
  addOnQuantity: (id: string) => `booking-addon-quantity-${id}`,
  
  // UI Components
  button: (id?: string) => `booking-button${id ? `-${id}` : ''}`,
  input: (id?: string) => `booking-input${id ? `-${id}` : ''}`,
  card: (id?: string) => `booking-card${id ? `-${id}` : ''}`,
  spinner: 'booking-spinner',
  badge: (id?: string) => `booking-badge${id ? `-${id}` : ''}`,
} as const;

