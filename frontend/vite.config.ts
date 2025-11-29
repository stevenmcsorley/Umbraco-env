import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import replace from '@rollup/plugin-replace';
import path from 'path';

export default defineConfig({
  plugins: [
    react(),
    replace({
      'process.env': JSON.stringify({}),
      'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV || 'production'),
      preventAssignment: true,
    }),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  define: {
    'process.env': JSON.stringify({}),
    'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV || 'production'),
    'global': 'globalThis',
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
    host: '0.0.0.0', // Allow access from Docker network
    hmr: {
      host: 'localhost' // HMR host for browser connection
    },
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
