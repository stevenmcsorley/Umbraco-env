import { create } from 'zustand';
import type { AvailabilityResponse, BookingResponse } from '../../types/domain.types';

export interface BookingState {
  selectedProductId: string | null;
  selectedDateRange: { from: Date | null; to: Date | null };
  availabilityResult: AvailabilityResponse | null;
  bookingPayload: any | null;
  confirmation: BookingResponse | null;
  ui: {
    loading: boolean;
    errorMessage: string | null;
  };
  setSelectedProductId: (id: string | null) => void;
  setSelectedDateRange: (from: Date | null, to: Date | null) => void;
  setAvailabilityResult: (result: AvailabilityResponse) => void;
  setBookingPayload: (payload: any) => void;
  setConfirmation: (confirmation: BookingResponse) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  reset: () => void;
}

const initialState = {
  selectedProductId: null,
  selectedDateRange: { from: null, to: null },
  availabilityResult: null,
  bookingPayload: null,
  confirmation: null,
  ui: {
    loading: false,
    errorMessage: null
  }
};

export const useBookingStore = create<BookingState>((set) => ({
  ...initialState,
  setSelectedProductId: (id) => set({ selectedProductId: id }),
  setSelectedDateRange: (from, to) => set({ selectedDateRange: { from, to } }),
  setAvailabilityResult: (result) => set({ availabilityResult: result }),
  setBookingPayload: (payload) => set({ bookingPayload: payload }),
  setConfirmation: (confirmation) => set({ confirmation }),
  setLoading: (loading) => set((state) => ({ ui: { ...state.ui, loading } })),
  setError: (error) => set((state) => ({ ui: { ...state.ui, errorMessage: error } })),
  reset: () => set(initialState)
}));

