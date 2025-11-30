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
   * Gets room availability from Umbraco hotel API with inventory pricing
   */
  private async getRoomAvailability(request: AvailabilityRequest, hotelId: string): Promise<AvailabilityResponse> {
    const fromDate = new Date(request.from);
    const toDate = new Date(request.to);
    
    // Format dates for API
    const fromStr = fromDate.toISOString().split('T')[0];
    const toStr = toDate.toISOString().split('T')[0];

    try {
      // Fetch inventory data with prices from the new inventory endpoint
      let inventoryData: { days: Array<{ date: string; price: number; availableQuantity: number; isAvailable: boolean }> } | null = null;
      try {
        // Format dates as yyyy-MM-dd (not ISO string with time)
        const fromStr = fromDate.toISOString().split('T')[0];
        const toStr = toDate.toISOString().split('T')[0];
        const inventoryResponse = await fetch(
          `${UMBRACO_API_BASE}/hotels/inventory/${request.productId}?from=${fromStr}&to=${toStr}`
        );
        
        if (inventoryResponse.ok) {
          inventoryData = await inventoryResponse.json() as { days: Array<{ date: string; price: number; availableQuantity: number; isAvailable: boolean }> };
        }
      } catch (error) {
        console.warn('Failed to fetch inventory data, falling back to room data:', error);
      }

      // Fetch room data from Umbraco API for fallback
      const response = await fetch(
        `${UMBRACO_API_BASE}/hotels/${hotelId}/availability?from=${fromStr}&to=${toStr}`
      );

      const data = response.ok ? await response.json() as {
        hotelId: string;
        from: string;
        to: string;
        rooms?: Array<{ roomId: string; roomName: string; available?: boolean }>;
      } : null;
      
      // Find the specific room in the response
      const roomData = data?.rooms?.find((r) => r.roomId === request.productId);

      // Generate calendar days based on inventory data
      const days: CalendarDay[] = [];
      let currentDate = new Date(fromDate);

      while (currentDate <= toDate) {
        const dateStr = currentDate.toISOString().split('T')[0];
        
        // Try to get price and availability from inventory data
        const inventoryDay = inventoryData?.days?.find(d => d.date === dateStr);
        
        if (inventoryDay) {
          // Use inventory data (has date-specific pricing)
          days.push({
            date: new Date(currentDate),
            available: inventoryDay.isAvailable && inventoryDay.availableQuantity > 0,
            price: inventoryDay.price,
            unitsAvailable: inventoryDay.availableQuantity
          });
        } else {
          // Fallback to room data or default
          const isAvailable = roomData?.available !== false;
          const fallbackPrice = await this.getPriceForDate(request.productId, currentDate, 'room');
          
          days.push({
            date: new Date(currentDate),
            available: isAvailable,
            price: fallbackPrice,
            unitsAvailable: isAvailable ? 1 : 0
          });
        }

        currentDate.setDate(currentDate.getDate() + 1);
      }

      return {
        productId: request.productId,
        from: request.from,
        to: request.to,
        days,
        currency: inventoryData?.days?.[0] ? 'GBP' : 'GBP'
      };
    } catch (error) {
      console.error('Error fetching room availability:', error);
      // Fallback to basic availability
      return this.generateBasicAvailability(request);
    }
  }

  /**
   * Gets event availability with inventory pricing
   */
  private async getEventAvailability(request: AvailabilityRequest, product: Product): Promise<AvailabilityResponse> {
    const eventDate = product.attributes?.eventDate;
    const fromDate = new Date(request.from);
    const toDate = new Date(request.to);
    
    // Try to fetch inventory data for events
    let inventoryData: { days: Array<{ date: string; price: number; availableQuantity: number; isAvailable: boolean }> } | null = null;
    try {
      // Format dates as yyyy-MM-dd (not ISO string with time)
      const fromStr = fromDate.toISOString().split('T')[0];
      const toStr = toDate.toISOString().split('T')[0];
      const inventoryResponse = await fetch(
        `${UMBRACO_API_BASE}/hotels/inventory/${request.productId}?from=${fromStr}&to=${toStr}`
      );
      
      if (inventoryResponse.ok) {
        inventoryData = await inventoryResponse.json() as { days: Array<{ date: string; price: number; availableQuantity: number; isAvailable: boolean }> };
      }
    } catch (error) {
      console.warn('Failed to fetch event inventory data:', error);
    }
    
    const days: CalendarDay[] = [];
    let currentDate = new Date(fromDate);

    while (currentDate <= toDate) {
      // Check if current date matches event date
      const eventDateObj = eventDate ? new Date(eventDate as string) : null;
      const isEventDate = eventDateObj ? this.isSameDay(eventDateObj, currentDate) : false;
      
      if (isEventDate) {
        const dateStr = currentDate.toISOString().split('T')[0];
        const inventoryDay = inventoryData?.days?.find(d => d.date === dateStr);
        
        if (inventoryDay) {
          // Use inventory data (has date-specific pricing and capacity)
          days.push({
            date: new Date(currentDate),
            available: inventoryDay.isAvailable && inventoryDay.availableQuantity > 0,
            price: inventoryDay.price,
            unitsAvailable: inventoryDay.availableQuantity
          });
        } else {
          // Fallback to product data
          const availableAttr = product.attributes?.available;
          const isUnavailable = (typeof availableAttr === 'boolean' && availableAttr === false) 
            || availableAttr === 0 
            || availableAttr === 'false';
          const isAvailable = !isUnavailable;
          const price = product.priceFrom ?? 0;
          const maxCapacity = typeof product.attributes?.maxCapacity === 'number' 
            ? product.attributes.maxCapacity 
            : 100;
          
          days.push({
            date: new Date(currentDate),
            available: isAvailable,
            price: price,
            unitsAvailable: isAvailable ? maxCapacity : 0
          });
        }
      } else {
        // Not an event date
        days.push({
          date: new Date(currentDate),
          available: false,
          price: undefined,
          unitsAvailable: 0
        });
      }

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
   * Gets price for a specific date from inventory (deprecated - now using bulk inventory fetch)
   * Kept for backward compatibility but inventory is now fetched in bulk in getRoomAvailability
   */
  private async getPriceForDate(productId: string, date: Date, productType: string): Promise<number | undefined> {
    // This method is now deprecated - prices come from inventory endpoint
    // Kept for any legacy code that might still call it
    try {
      const dateStr = date.toISOString().split('T')[0];
      const inventoryResponse = await fetch(
        `${UMBRACO_API_BASE}/hotels/inventory/${productId}?from=${dateStr}&to=${dateStr}`
      );
      
      if (inventoryResponse.ok) {
        const inventoryData = await inventoryResponse.json() as { days?: Array<{ date: string; price: number; availableQuantity: number; isAvailable: boolean }> };
        const day = inventoryData.days?.[0];
        if (day) {
          return day.price;
        }
      }
      
      // Fallback to product base price
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
      const response = await fetch(`${UMBRACO_API_BASE}/hotels/${hotelId}/addons`);
      if (!response.ok) {
        console.warn(`Failed to fetch add-ons for hotel ${hotelId}: ${response.status}`);
        return [];
      }

      const addOnsData = await response.json() as Array<{
        id: string;
        name: string;
        description?: string;
        price: number;
        pricingType: string;
        image?: string;
      }>;

      return addOnsData.map(addOn => ({
        id: addOn.id,
        name: addOn.name,
        description: addOn.description,
        price: addOn.price,
        type: (addOn.pricingType || 'one-time') as 'one-time' | 'per-night' | 'per-person' | 'per-unit',
        available: true,
        image: addOn.image
      }));
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

