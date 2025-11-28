// Entry point for embeddable booking engine only
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
}) => {
  const container = document.getElementById(config.containerId);
  if (!container) {
    console.error(`Container ${config.containerId} not found`);
    return;
  }

  const root = ReactDOM.createRoot(container);
  root.render(
    <React.StrictMode>
      <BookingApp
        hotelId={config.hotelId}
        apiBaseUrl={config.apiBaseUrl}
        defaultProductId={config.productId}
      />
    </React.StrictMode>
  );
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

