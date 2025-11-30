// Entry point for embeddable booking engine only
// Polyfill for process.env (needed for browser builds)
if (typeof (window as any).process === 'undefined') {
  (window as any).process = { env: {} };
}

import React from 'react';
import ReactDOM from 'react-dom/client';
import { BookingApp } from './booking-engine/app/BookingApp';
import './index.css';

// Global function to initialize booking engine
(window as any).initBookingEngine = (config: {
  containerId: string;
  productId: string;
  hotelId: string;
  apiBaseUrl: string;
  user?: {
    userId: string;
    email: string;
    firstName: string;
    lastName: string;
    phone?: string;
  } | null;
}) => {
  console.log('[initBookingEngine] Starting initialization with config:', config);
  const container = document.getElementById(config.containerId);
  if (!container) {
    console.error(`[initBookingEngine] Container ${config.containerId} not found`);
    return;
  }

  console.log('[initBookingEngine] Container found, creating React root');
  try {
    const root = ReactDOM.createRoot(container);
    console.log('[initBookingEngine] React root created, rendering BookingApp');
    root.render(
      <React.StrictMode>
        <BookingApp
          hotelId={config.hotelId}
          apiBaseUrl={config.apiBaseUrl}
          defaultProductId={config.productId}
          user={config.user}
        />
      </React.StrictMode>
    );
    console.log('[initBookingEngine] React render called successfully');
  } catch (error) {
    console.error('[initBookingEngine] Error rendering React:', error);
    container.innerHTML = `
      <div style="padding: 20px; border: 2px solid red; background: #fee; color: #c00;">
        <h3>Error Loading Booking Engine</h3>
        <p>${error instanceof Error ? error.message : String(error)}</p>
        <pre>${error instanceof Error ? error.stack : ''}</pre>
      </div>
    `;
  }
};

// Auto-initialize if data attributes are present
document.addEventListener('DOMContentLoaded', () => {
  const container = document.getElementById('booking-engine-root');
  if (container) {
    const productId = container.getAttribute('data-product-id');
    const hotelId = container.getAttribute('data-hotel-id');
    const apiBaseUrl = container.getAttribute('data-api-base-url') || '/engine';

    if (productId && hotelId) {
      (window as any).initBookingEngine({
        containerId: 'booking-engine-root',
        productId,
        hotelId,
        apiBaseUrl
      });
    }
  }
});

