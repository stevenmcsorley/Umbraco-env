import type { Hotel, Room, Offer } from '../../shared-types/domain.types';

const API_BASE = '/api/hotels';

export const hotelsApi = {
  async getHotels(): Promise<Hotel[]> {
    const response = await fetch(API_BASE);
    if (!response.ok) throw new Error('Failed to fetch hotels');
    return response.json();
  },

  async getHotel(id: string): Promise<Hotel> {
    const response = await fetch(`${API_BASE}/${id}`);
    if (!response.ok) throw new Error('Failed to fetch hotel');
    return response.json();
  },

  async getRooms(hotelId: string): Promise<Room[]> {
    const response = await fetch(`${API_BASE}/${hotelId}/rooms`);
    if (!response.ok) throw new Error('Failed to fetch rooms');
    return response.json();
  },

  async getOffers(hotelId: string): Promise<Offer[]> {
    const response = await fetch(`${API_BASE}/${hotelId}/offers`);
    if (!response.ok) throw new Error('Failed to fetch offers');
    return response.json();
  }
};

