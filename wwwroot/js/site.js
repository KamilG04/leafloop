// Pełna ścieżka: wwwroot/js/site.js

// === POPRAWIONY IMPORT ===
// Zakładamy, że auth.js jest w podfolderze 'utils' względem site.js
import { getJwtToken, getAuthHeaders, handleApiResponse } from './utils/auth.js';
// === KONIEC POPRAWKI ===

/**
 * Theme toggle functionality
 */
document.addEventListener('DOMContentLoaded', function() {
    const themeToggle = document.getElementById('theme-toggle');
    if (!themeToggle) {
        console.log("Theme toggle button not found.");
        return;
    }
    console.log("Theme toggle button found, initializing.");

    const initialTheme = getInitialTheme();
    console.log("Initial theme:", initialTheme);
    applyTheme(initialTheme);

    themeToggle.addEventListener('click', function() {
        const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        console.log(`Theme toggled from ${currentTheme} to ${newTheme}`);

        applyTheme(newTheme);
        saveThemePreference(newTheme);
    });
});

/**
 * Gets the initial theme from localStorage or system preference.
 * @returns {string} 'dark' or 'light'
 */
function getInitialTheme() {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
        console.log("Initial theme from localStorage:", savedTheme);
        return savedTheme;
    }
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        console.log("Initial theme from system preference: dark");
        return 'dark';
    }
    console.log("Initial theme: default light");
    return 'light';
}

/**
 * Applies the theme to the document and updates toggle button icon.
 * @param {string} theme 'dark' or 'light'
 */
function applyTheme(theme) {
    document.documentElement.setAttribute('data-bs-theme', theme);
    console.log(`Applied theme: ${theme}`);
    const themeToggle = document.getElementById('theme-toggle');
    if (themeToggle) {
        const toggleIcon = themeToggle.querySelector('i');
        if (toggleIcon) {
            toggleIcon.className = theme === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-stars';
            toggleIcon.title = theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode';
        }
    }
}

/**
 * Saves theme preference to localStorage and attempts to save to user profile via API.
 * Uses imported functions from auth.js.
 * @param {string} theme 'dark' or 'light'
 */
async function saveThemePreference(theme) {
    localStorage.setItem('theme', theme);
    console.log(`Theme preference '${theme}' saved to localStorage.`);

    const token = getJwtToken(); // Używa zaimportowanej funkcji

    if (token) {
        console.log("User is authenticated, attempting to save theme preference via API...");
        try {
            const headers = getAuthHeaders(true); // Używa zaimportowanej funkcji
            const body = JSON.stringify({ theme: theme });

            console.log("Calling PUT /api/preferences/theme with body:", body);
            const response = await fetch('/api/preferences/theme', {
                method: 'PUT',
                headers: headers,
                body: body
            });

            await handleApiResponse(response); // Używa zaimportowanej funkcji
            console.log("Theme preference successfully saved to user profile via API.");

        } catch (err) {
            console.warn('Failed to save theme preference to user profile via API:', err.message);
        }
    } else {
        console.log("User not authenticated, skipping API save for theme preference.");
    }
}

// Usunięto lokalną definicję getJwtToken

/**
 * Sets up accessibility features.
 */
function setupAccessibilityFeatures() {
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Tab') {
            document.body.classList.add('keyboard-navigation');
        }
    });
    document.addEventListener('mousedown', function() {
        document.body.classList.remove('keyboard-navigation');
    });
    console.log("Accessibility features set up.");
}

document.addEventListener('DOMContentLoaded', setupAccessibilityFeatures);

