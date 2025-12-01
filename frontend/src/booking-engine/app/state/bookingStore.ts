import { create } from 'zustand';
import type { AvailabilityResponse, BookingResponse, AddOn } from '../../types/domain.types';

export interface SelectedAddOn {
  addOnId: string;
  quantity: number;
}

export interface SelectedOffer {
  offerId: string;
}

export interface SelectedEvent {
  eventId: string;
}

export interface BookingState {
  selectedHotelId: string | null;
  selectedProductId: string | null;
  selectedDateRange: { from: Date | null; to: Date | null };
  availabilityResult: AvailabilityResponse | null;
  bookingPayload: any | null;
  confirmation: BookingResponse | null;
  availableAddOns: AddOn[];
  selectedAddOns: SelectedAddOn[];
  selectedEvents: SelectedEvent[];
  selectedOffer: SelectedOffer | null;
  ui: {
    loading: boolean;
    errorMessage: string | null;
  };
  setSelectedHotelId: (id: string | null) => void;
  setSelectedProductId: (id: string | null) => void;
  setSelectedDateRange: (from: Date | null, to: Date | null) => void;
  setAvailabilityResult: (result: AvailabilityResponse) => void;
  setBookingPayload: (payload: any) => void;
  setConfirmation: (confirmation: BookingResponse) => void;
  setAvailableAddOns: (addOns: AddOn[]) => void;
  toggleAddOn: (addOnId: string, quantity?: number) => void;
  toggleEvent: (eventId: string) => void;
  setSelectedOffer: (offer: SelectedOffer | null) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  reset: () => void;
}

const initialState = {
  selectedHotelId: null,
  selectedProductId: null,
  selectedDateRange: { from: null, to: null },
  availabilityResult: null,
  bookingPayload: null,
  confirmation: null,
  availableAddOns: [],
  selectedAddOns: [],
  selectedEvents: [],
  selectedOffer: null,
  ui: {
    loading: false,
    errorMessage: null
  }
};

export const useBookingStore = create<BookingState>((set) => ({
  ...initialState,
  setSelectedHotelId: (id) => set({ selectedHotelId: id, selectedProductId: null }), // Clear product when hotel changes
  setSelectedProductId: (id) => set({ selectedProductId: id }),
  setSelectedDateRange: (from, to) => set({ selectedDateRange: { from, to } }),
  setAvailabilityResult: (result) => set({ availabilityResult: result }),
  setBookingPayload: (payload) => set({ bookingPayload: payload }),
  setConfirmation: (confirmation) => set({ confirmation }),
  setAvailableAddOns: (addOns) => set({ availableAddOns: addOns }),
  setSelectedOffer: (offer) => set({ selectedOffer: offer }),
  toggleEvent: (eventId: string) => set((state) => {
    const existingIndex = state.selectedEvents.findIndex((e: SelectedEvent) => e.eventId === eventId);
    if (existingIndex >= 0) {
      // Remove event
      return {
        selectedEvents: state.selectedEvents.filter((e: SelectedEvent) => e.eventId !== eventId)
      };
    } else {
      // Add event
      return {
        selectedEvents: [...state.selectedEvents, { eventId }]
      };
    }
  }),
  toggleAddOn: (addOnId, quantity = 1) => set((state) => {
    const existingIndex = state.selectedAddOns.findIndex(a => a.addOnId === addOnId);
    if (existingIndex >= 0) {
      // Remove if quantity is 0 or toggle off
      if (quantity === 0) {
        return {
          selectedAddOns: state.selectedAddOns.filter(a => a.addOnId !== addOnId)
        };
      }
      // Update quantity
      const updated = [...state.selectedAddOns];
      updated[existingIndex] = { addOnId, quantity };
      return { selectedAddOns: updated };
    } else {
      // Add new
      return {
        selectedAddOns: [...state.selectedAddOns, { addOnId, quantity }]
      };
    }
  }),
  setLoading: (loading) => set((state) => ({ ui: { ...state.ui, loading } })),
  setError: (error) => set((state) => ({ ui: { ...state.ui, errorMessage: error } })),
  reset: () => set(initialState)
}));
