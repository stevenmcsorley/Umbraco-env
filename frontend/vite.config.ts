import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    lib: {
      entry: path.resolve(__dirname, 'src/booking-engine-entry.tsx'),
      name: 'BookingEngine',
      fileName: 'booking-engine',
      formats: ['iife'],
    },
    rollupOptions: {
      external: [],
      output: {
        globals: {},
      },
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:44372',
        changeOrigin: true
      },
      '/engine': {
        target: 'http://localhost:3001',
        changeOrigin: true
      }
    }
  }
});
