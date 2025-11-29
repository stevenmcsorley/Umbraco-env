import type { AvailabilityRequest, AvailabilityResponse, CalendarDay, Product, AddOn } from '../types/domain.types';

// In Docker, use service name; locally, use localhost
const UMBRACO_API_BASE = process.env.UMBRACO_API_BASE || 
  (process.env.NODE_ENV === 'production' 
    ? 'http://mydockerproject:8080/api' 
    : 'http://localhost:44372/api');

export class UmbracoAdapter {
  /**
   * Fetches availability from Umbraco API for a product (room or event)
   */
  async getAvailability(request: AvailabilityRequest): Promise<AvailabilityResponse> {
    // First, determine if this is a room or event by checking the product
    const product = await this.getProduct(request.productId);
    
    if (!product) {
      throw new Error(`Product ${request.productId} not found`);
    }

    // For rooms, use hotel availability endpoint
    if (product.type === 'room' && product.hotelId) {
      return this.getRoomAvailability(request, product.hotelId);
    }

    // For events, use event-specific logic
    if (product.type === 'event') {
      return this.getEventAvailability(request, product);
    }

    // Fallback: generate basic availability
    return this.generateBasicAvailability(request);
  }

  /**
   * Gets product information from Umbraco
   */
  async getProduct(productId: string): Promise<Product | null> {
    try {
      // Try to fetch as room first
      const hotelsResponse = await fetch(`${UMBRACO_API_BASE}/hotels`);
      if (hotelsResponse.ok) {
        const hotels = await hotelsResponse.json() as Array<{ id: string; [key: string]: any }>;
        for (const hotel of hotels) {
          const roomsResponse = await fetch(`${UMBRACO_API_BASE}/hotels/${hotel.id}/rooms`);
          if (roomsResponse.ok) {
            const rooms = await roomsResponse.json() as Array<{ id: string; [key: string]: any }>;
            const room = rooms.find((r) => r.id === productId);
            if (room) {
              return {
                id: room.id,
                type: 'room',
                title: room.name,
                description: room.description,
                images: room.roomImages || (room.heroImage ? [room.heroImage] : []),
                priceFrom: room.priceFrom,
                hotelId: hotel.id,
                attributes: {
                  maxOccupancy: room.maxOccupancy,
                  roomType: room.roomType,
                  size: room.size,
                  bedType: room.bedType
                }
              };
            }
          }
        }
      }

      // Try to fetch as event
      const hotelsResponse2 = await fetch(`${UMBRACO_API_BASE}/hotels`);
      if (hotelsResponse2.ok) {
        const hotels = await hotelsResponse2.json() as Array<{ id: string; [key: string]: any }>;
        for (const hotel of hotels) {
          const eventsResponse = await fetch(`${UMBRACO_API_BASE}/hotels/${hotel.id}/events`);
          if (eventsResponse.ok) {
            const events = await eventsResponse.json() as Array<{ id: string; [key: string]: any }>;
            const event = events.find((e) => e.id === productId);
            if (event) {
              return {
                id: event.id,
                type: 'event',
                title: event.name,
                description: event.description,
                images: event.image ? [event.image] : [],
                priceFrom: event.price,
                hotelId: hotel.id,
                attributes: {
                  eventDate: event.eventDate,
                  location: event.location
                }
              };
            }
          }
        }
      }

      return null;
    } catch (error) {
      console.error('Error fetching product:', error);
      return null;
    }
  }

  /**
   * Gets room availability from Umbraco hotel API
   */
  private async getRoomAvailability(request: AvailabilityRequest, hotelId: string): Promise<AvailabilityResponse> {
    const fromDate = new Date(request.from);
    const toDate = new Date(request.to);
    
    // Format dates for API
    const fromStr = fromDate.toISOString().split('T')[0];
    const toStr = toDate.toISOString().split('T')[0];

    try {
      // Fetch availability from Umbraco API
      const response = await fetch(
        `${UMBRACO_API_BASE}/hotels/${hotelId}/availability?from=${fromStr}&to=${toStr}`
      );

      if (!response.ok) {
        throw new Error('Failed to fetch availability from Umbraco');
      }

      const data = await response.json() as {
        hotelId: string;
        from: string;
        to: string;
        rooms?: Array<{ roomId: string; roomName: string; available?: boolean }>;
      };
      
      // Find the specific room in the response
      const roomData = data.rooms?.find((r) => r.roomId === request.productId);
      
      if (!roomData) {
        // Room not found in availability, generate basic availability
        return this.generateBasicAvailability(request);
      }

      // Generate calendar days based on availability
      const days: CalendarDay[] = [];
      let currentDate = new Date(fromDate);

      while (currentDate <= toDate) {
        // For now, assume available if room exists (actual availability logic would come from booking system)
        // In a real implementation, this would check against actual booking data
        const isAvailable = roomData.available !== false;
        const price = await this.getPriceForDate(request.productId, currentDate, 'room');

        days.push({
          date: new Date(currentDate),
          available: isAvailable,
          price: price,
          unitsAvailable: isAvailable ? 1 : 0
        });

        currentDate.setDate(currentDate.getDate() + 1);
      }

      return {
        productId: request.productId,
        from: request.from,
        to: request.to,
        days,
        currency: 'GBP'
      };
    } catch (error) {
      console.error('Error fetching room availability:', error);
      // Fallback to basic availability
      return this.generateBasicAvailability(request);
    }
  }

  /**
   * Gets event availability
   */
  private async getEventAvailability(request: AvailabilityRequest, product: Product): Promise<AvailabilityResponse> {
    const eventDate = product.attributes?.eventDate;
    const fromDate = new Date(request.from);
    const toDate = new Date(request.to);
    
    const days: CalendarDay[] = [];
    let currentDate = new Date(fromDate);

      while (currentDate <= toDate) {
        // Check if current date matches event date
        const eventDateObj = eventDate ? new Date(eventDate as string) : null;
        const isEventDate = eventDateObj ? this.isSameDay(eventDateObj, currentDate) : false;
        const availableAttr = product.attributes?.available;
        // Check if available is explicitly false (boolean) or falsy string/number
        // Type guard: check if it's a boolean false, or falsy values
        const isUnavailable = (typeof availableAttr === 'boolean' && availableAttr === false) 
          || availableAttr === 0 
          || availableAttr === 'false';
        const isAvailable = isEventDate && !isUnavailable;
        const price = isEventDate ? (product.priceFrom ?? 0) : undefined;

        const maxCapacity = typeof product.attributes?.maxCapacity === 'number' 
          ? product.attributes.maxCapacity 
          : 100;
        days.push({
          date: new Date(currentDate),
          available: isAvailable,
          price: price,
          unitsAvailable: isAvailable ? maxCapacity : 0
        });

      currentDate.setDate(currentDate.getDate() + 1);
    }

    return {
      productId: request.productId,
      from: request.from,
      to: request.to,
      days,
      currency: 'GBP'
    };
  }

  /**
   * Gets price for a specific date (can be extended to support dynamic pricing)
   */
  private async getPriceForDate(productId: string, date: Date, productType: string): Promise<number | undefined> {
    // For now, return base price. In future, this could check:
    // - Seasonal pricing
    // - Dynamic pricing rules
    // - Special offers
    // - Day-of-week pricing
    
    try {
      const product = await this.getProduct(productId);
      return product?.priceFrom;
    } catch {
      return undefined;
    }
  }

  /**
   * Generates basic availability when specific data isn't available
   */
  private generateBasicAvailability(request: AvailabilityRequest): AvailabilityResponse {
    const days: CalendarDay[] = [];
    const fromDate = new Date(request.from);
    const toDate = new Date(request.to);
    let currentDate = new Date(fromDate);

    while (currentDate <= toDate) {
      // Default: available with no price set
      days.push({
        date: new Date(currentDate),
        available: true,
        price: undefined,
        unitsAvailable: 1
      });

      currentDate.setDate(currentDate.getDate() + 1);
    }

    return {
      productId: request.productId,
      from: request.from,
      to: request.to,
      days,
      currency: 'GBP'
    };
  }

  /**
   * Gets add-ons for a hotel/product
   */
  async getAddOns(hotelId: string): Promise<AddOn[]> {
    try {
      // In future, this would fetch from Umbraco add-ons content
      // For now, return empty array - can be extended when add-ons content type is created
      return [];
    } catch (error) {
      console.error('Error fetching add-ons:', error);
      return [];
    }
  }

  private isSameDay(date1: Date, date2: Date): boolean {
    return date1.getFullYear() === date2.getFullYear() &&
           date1.getMonth() === date2.getMonth() &&
           date1.getDate() === date2.getDate();
  }
}

