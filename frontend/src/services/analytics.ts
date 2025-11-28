// Analytics manager stub - no real GA integration per PRD

export class AnalyticsManager {
  private static tags: string[] = [];

  static initialize(tags: string[]): void {
    this.tags = tags;
    console.log('[Analytics] Initialized with tags:', tags);
    // Tags stored but not used in stub implementation
  }

  static trackEvent(eventName: string, payload?: Record<string, unknown>): void {
    console.log('[Analytics] Event:', eventName, payload);
    // Stub implementation - no real GA integration
  }

  static trackBookingStart(hotelId: string, productId: string): void {
    this.trackEvent('booking:start', { hotelId, productId });
  }

  static trackAvailabilityChecked(productId: string, from: string, to: string): void {
    this.trackEvent('booking:availability_checked', { productId, from, to });
  }

  static trackBookingConfirmed(bookingId: string): void {
    this.trackEvent('booking:confirmed', { bookingId });
  }
}

