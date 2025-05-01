// darkModeToggle.js (entry point)
import React, { StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import DarkModeToggle from './DarkModeToggleComponent';

// Initialize the dark mode toggle
const container = document.getElementById('dark-mode-toggle-container');
if (container) {
    const root = ReactDOM.createRoot(container);
    root.render(
        <StrictMode>
            <DarkModeToggle />
        </StrictMode>
    );
}