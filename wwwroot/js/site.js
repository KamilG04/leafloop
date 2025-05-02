// site.js - Main JavaScript file for LeafLoop

/**
 * Theme toggle functionality
 * Sets up dark/light mode switching functionality
 */
document.addEventListener('DOMContentLoaded', function() {
    const themeToggle = document.getElementById('theme-toggle');
    if (!themeToggle) return;

    const initialTheme = getInitialTheme();
    applyTheme(initialTheme);

    // Toggle theme on click
    themeToggle.addEventListener('click', function() {
        const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

        applyTheme(newTheme);
        saveThemePreference(newTheme);
    });
});

/**
 * Gets the initial theme from localStorage or system preference
 * @returns {string} 'dark' or 'light'
 */
function getInitialTheme() {
    // Check for saved theme preference
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
        return savedTheme;
    }

    // Check for system preference
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        return 'dark';
    }

    // Default theme
    return 'light';
}

/**
 * Applies the theme to the document and updates toggle button
 * @param {string} theme 'dark' or 'light'
 */
function applyTheme(theme) {
    document.documentElement.setAttribute('data-bs-theme', theme);

    // Update theme toggle icon
    const themeToggle = document.getElementById('theme-toggle');
    if (themeToggle) {
        const toggleIcon = themeToggle.querySelector('i');
        if (toggleIcon) {
            toggleIcon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        }
    }
}

/**
 * Saves theme preference to localStorage
 * @param {string} theme 'dark' or 'light'
 */
function saveThemePreference(theme) {
    localStorage.setItem('theme', theme);

    // Call API to save user preference (if authenticated)
    const token = getJwtToken();
    if (token) {
        fetch('/api/preferences/theme', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ theme: theme })
        }).catch(err => {
            console.warn('Failed to save theme preference to user profile', err);
        });
    }
}

/**
 * Gets the JWT token from cookies
 * @returns {string|null} JWT token or null if not found
 */
function getJwtToken() {
    const cookie = document.cookie.split('; ').find(row => row.startsWith('jwt_token='));
    return cookie ? cookie.split('=')[1] : null;
}

/**
 * Sets up accessibility features for the site
 */
function setupAccessibilityFeatures() {
    // Add keyboard focus visible class
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Tab') {
            document.body.classList.add('keyboard-navigation');
        }
    });

    document.addEventListener('mousedown', function() {
        document.body.classList.remove('keyboard-navigation');
    });
}

// Initialize accessibility features
document.addEventListener('DOMContentLoaded', setupAccessibilityFeatures);