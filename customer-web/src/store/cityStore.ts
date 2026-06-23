import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { CityOption } from '@/types/cities';

interface CityState {
  city: CityOption | null;
  setCity: (city: CityOption) => void;
  clearCity: () => void;
}

export const useCityStore = create<CityState>()(
  persist(
    (set) => ({
      city: null,
      setCity: (city) => set({ city }),
      clearCity: () => set({ city: null }),
    }),
    { name: 'gridtickets-city' }
  )
);
