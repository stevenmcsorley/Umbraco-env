import type { BookingRequest, BookingResponse } from '../types/domain.types';
import { AvailabilityService } from './AvailabilityService';

// In Docker, use service name; locally, use localhost
const UMBRACO_API_BASE = process.env.UMBRACO_API_BASE || 
  (process.env.NODE_ENV === 'production' 
    ? 'http://mydockerproject:8080/api' 
    : 'http://localhost:44372/api');

export class BookingService {
  static async createBooking(request: BookingRequest): Promise<BookingResponse> {
    // Calculate total price including add-ons
    const product = await AvailabilityService.getProduct(request.productId);
    const fromDate = new Date(request.from);
    const toDate = new Date(request.to);
    
    // Calculate number of nights/days
    const nights = Math.ceil((toDate.getTime() - fromDate.getTime()) / (1000 * 60 * 60 * 24));
    
    // Get base price from availability
    const availability = await AvailabilityService.getAvailability(
      request.productId,
      fromDate,
      toDate
    );
    
    const basePrice = availability.days.reduce((sum, day) => sum + (day.price || 0), 0);
    
    // Calculate add-on prices
    let addOnsTotal = 0;
    const addOnsDetails: BookingResponse['addOns'] = [];
    
    if (request.addOns && request.addOns.length > 0) {
      // Fetch add-ons from Umbraco
      const { UmbracoAdapter } = await import('../adapters/UmbracoAdapter');
      const umbracoAdapter = new UmbracoAdapter();
      const hotelId = product?.hotelId || '';
      const availableAddOns = await umbracoAdapter.getAddOns(hotelId);
      
      for (const addOnRequest of request.addOns) {
        const addOn = availableAddOns.find(a => a.id === addOnRequest.addOnId);
        if (!addOn) {
          console.warn(`Add-on ${addOnRequest.addOnId} not found, skipping`);
          continue;
        }
        
        // Calculate price based on pricing type
        let addOnTotal = 0;
        if (addOn.type === 'per-night') {
          addOnTotal = addOn.price * addOnRequest.quantity * (nights > 0 ? nights : 1);
        } else if (addOn.type === 'per-person') {
          addOnTotal = addOn.price * addOnRequest.quantity * (request.quantity || 1);
        } else {
          // one-time or per-unit
          addOnTotal = addOn.price * addOnRequest.quantity;
        }
        
        addOnsTotal += addOnTotal;
        addOnsDetails.push({
          addOnId: addOnRequest.addOnId,
          name: addOn.name,
          quantity: addOnRequest.quantity,
          price: addOnTotal
        });
      }
    }

    const totalPrice = basePrice + addOnsTotal;

    // Determine product type
    const productType = product?.type === 'room' ? 'Room' : 'Event';
    
    // Prepare additional data (add-ons, events, offers) as JSON
    const additionalData = request.addOns && request.addOns.length > 0
      ? JSON.stringify({ addOns: request.addOns })
      : null;

    // If user is logged in, use userId; otherwise use guestDetails
    // If guestDetails is not provided but userId is, we'll need to fetch user details from Umbraco
    // For now, if userId is provided, we'll use it and let the API handle guest details
    const guestDetails = request.guestDetails || {};
    
    // Call Umbraco API to create booking
    const umbracoRequest = {
      productId: request.productId,
      productType: productType,
      checkIn: fromDate.toISOString(),
      checkOut: productType === 'Room' ? toDate.toISOString() : null,
      quantity: request.quantity || 1,
      guestFirstName: guestDetails.firstName || '',
      guestLastName: guestDetails.lastName || '',
      guestEmail: guestDetails.email || '',
      guestPhone: guestDetails.phone || null,
      totalPrice: totalPrice,
      currency: availability.currency || 'GBP',
      additionalData: additionalData,
      userId: request.userId || null // Use userId from request if provided
    };

    try {
      const response = await fetch(`${UMBRACO_API_BASE}/bookings`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(umbracoRequest)
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ error: 'Failed to create booking' }));
        throw new Error(errorData.error || `HTTP ${response.status}: Failed to create booking`);
      }

      const bookingData = await response.json();

      // Map Umbraco response to BookingResponse format
      return {
        bookingId: bookingData.bookingId || bookingData.bookingReference,
        productId: bookingData.productId,
        from: new Date(bookingData.checkIn),
        to: bookingData.checkOut ? new Date(bookingData.checkOut) : new Date(bookingData.checkIn),
        guestDetails: request.guestDetails,
        status: bookingData.status?.toLowerCase() || 'confirmed',
        createdAt: new Date(bookingData.createdAt),
        totalPrice: bookingData.totalPrice,
        currency: bookingData.currency || 'GBP',
        addOns: addOnsDetails.length > 0 ? addOnsDetails : undefined
      };
    } catch (error) {
      console.error('Error creating booking via Umbraco API:', error);
      throw error;
    }
  }
}

