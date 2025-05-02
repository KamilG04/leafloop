// wwwroot/js/site.js

/**
 * LeafLoop Main JavaScript File
 * This file contains global site functionality
 */

// In wwwroot/js/site.js

// Theme toggle functionality
document.addEventListener('DOMContentLoaded', function() {
    const themeToggle = document.getElementById('theme-toggle');
    if (!themeToggle) return;

    const toggleIcon = themeToggle.querySelector('i');
    if (!toggleIcon) return;

    // Set up initial theme
    function setTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        if (toggleIcon) {
            toggleIcon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        }
        localStorage.setItem('theme', theme);
    }

    // Get initial theme from localStorage or user preference
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
        setTheme(savedTheme);
    } else if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        setTheme('dark');
    } else {
        setTheme('light');
    }

    // Toggle theme on click
    themeToggle.addEventListener('click', function() {
        const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
        setTheme(currentTheme === 'dark' ? 'light' : 'dark');
    });
});
/**
 * Sets up the theme toggle functionality
 */
function setupThemeToggle() {
    const themeToggle = document.getElementById('theme-toggle');
    if (!themeToggle) return; // Exit if toggle button doesn't exist

    const darkIcon = document.getElementById('dark-icon');
    const lightIcon = document.getElementById('light-icon');

    // Check for saved theme preference or set based on system preference
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
        document.documentElement.setAttribute('data-bs-theme', savedTheme);
        updateThemeIcons(savedTheme);
    } else {
        // Check for system preference
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            updateThemeIcons('dark');
        }
    }

    // Toggle theme on button click
    themeToggle.addEventListener('click', function() {
        const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

        document.documentElement.setAttribute('data-bs-theme', newTheme);
        localStorage.setItem('theme', newTheme);
        updateThemeIcons(newTheme);
    });

    function updateThemeIcons(theme) {
        if (!darkIcon || !lightIcon) return;

        if (theme === 'dark') {
            darkIcon.style.display = 'none';
            lightIcon.style.display = 'inline-block';
        } else {
            darkIcon.style.display = 'inline-block';
            lightIcon.style.display = 'none';
        }
    }

    // Also listen for system preference changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', event => {
        if (!localStorage.getItem('theme')) { // Only react if user hasn't set preference
            const newTheme = event.matches ? 'dark' : 'light';
            document.documentElement.setAttribute('data-bs-theme', newTheme);
            updateThemeIcons(newTheme);
        }
    });
}

/**
 * Sets up accessibility features for the site
 */
function setupAccessibilityFeatures() {
    // Focus visible handling
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Tab') {
            document.body.classList.add('keyboard-navigation');
        }
    });

    document.addEventListener('mousedown', function() {
        document.body.classList.remove('keyboard-navigation');
    });

    // You can add more accessibility features here
}