import React, { StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import NearbyItems from '../components/nearbyItems.js';

// nearby lookby initialization 
document.addEventListener('DOMContentLoaded', function() {
    const container = document.getElementById('react-nearby-items');
    if (container) {
        const root = ReactDOM.createRoot(container);
        root.render(
            <StrictMode>
                <NearbyItems showLocationPicker={true} />
            </StrictMode>
        );
        console.log('NearbyItems component initialized');
    }
});