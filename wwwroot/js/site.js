// site.js - Enhanced LeafLoop animations and interactions

// Import auth utilities
import { getJwtToken, getAuthHeaders, handleApiResponse } from './utils/auth.js';

document.addEventListener('DOMContentLoaded', function() {
    // Initialize all animations and UI enhancements
    initScrollAnimations();
    initNavbarScroll();
    initItemCardHoverEffects();
    initCounterAnimations();
    initImageLazyLoading();
    setupAccessibilityFeatures();

    // Initialize the JWT token if needed
    if (typeof getJwtToken === 'function') {
        getJwtToken(); // This will sync the token if needed
    }

    // Remove page loader
    const pageLoader = document.getElementById('page-loader');
    if (pageLoader) {
        setTimeout(() => {
            pageLoader.style.opacity = '0';
            setTimeout(() => {
                pageLoader.remove();
            }, 500);
        }, 300);
    }
});

// Handle navbar appearance on scroll
function initNavbarScroll() {
    const navbar = document.querySelector('.navbar');
    if (!navbar) return;

    window.addEventListener('scroll', function() {
        if (window.scrollY > 50) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
    });

    // Check initial scroll position (for page refreshes)
    if (window.scrollY > 50) {
        navbar.classList.add('scrolled');
    }
}

// Animate elements when they come into view
function initScrollAnimations() {
    // Select all elements with fade-in-section class
    const fadeElements = document.querySelectorAll('.fade-in-section');

    // Setup Intersection Observer
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('is-visible');
                // Optionally unobserve after animation
                // observer.unobserve(entry.target);
            } else {
                // Optional: Remove the class when element is out of view
                // entry.target.classList.remove('is-visible');
            }
        });
    }, {
        root: null, // viewport
        threshold: 0.1, // trigger when 10% visible
        rootMargin: '0px 0px -50px 0px' // trigger slightly before element enters view
    });

    // Observe each fade element
    fadeElements.forEach(el => {
        observer.observe(el);
    });

    // Handle staggered items (grids)
    const staggerContainers = document.querySelectorAll('.stagger-container');

    staggerContainers.forEach(container => {
        const staggerItems = container.querySelectorAll('.stagger-item');

        const staggerObserver = new IntersectionObserver((entries) => {
            if (entries[0].isIntersecting) {
                staggerItems.forEach((item, index) => {
                    setTimeout(() => {
                        item.classList.add('is-visible');
                    }, 100 * index); // 100ms delay between each item
                });
                staggerObserver.unobserve(container);
            }
        }, {
            threshold: 0.1
        });

        staggerObserver.observe(container);
    });
}

// Add advanced hover effects to item cards
function initItemCardHoverEffects() {
    const itemCards = document.querySelectorAll('.item-card');

    itemCards.forEach(card => {
        card.addEventListener('mouseenter', function() {
            this.style.zIndex = "10";
        });

        card.addEventListener('mouseleave', function() {
            this.style.zIndex = "1";
        });
    });
}

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

// Animate counter elements
function initCounterAnimations() {
    const counterElements = document.querySelectorAll('.counter-value');

    const counterObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const target = entry.target;
                const targetValue = parseInt(target.getAttribute('data-target'), 10);
                const duration = 2000; // 2 seconds
                const stepTime = 50; // update every 50ms
                const steps = duration / stepTime;
                const increment = targetValue / steps;
                let current = 0;

                const timer = setInterval(() => {
                    current += increment;
                    target.textContent = Math.ceil(current);

                    if (current >= targetValue) {
                        clearInterval(timer);
                        target.textContent = targetValue;
                    }
                }, stepTime);

                counterObserver.unobserve(target);
            }
        });
    }, {
        threshold: 0.5
    });

    counterElements.forEach(el => {
        // Store the target value as a data attribute
        const targetValue = el.textContent;
        el.setAttribute('data-target', targetValue);
        el.textContent = '0';

        counterObserver.observe(el);
    });
}

// Implement image lazy loading with fade-in effect
function initImageLazyLoading() {
    const lazyImages = document.querySelectorAll('img[data-src]');

    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.style.opacity = 0;

                    img.onload = function() {
                        img.style.transition = 'opacity 0.5s ease';
                        img.style.opacity = 1;
                        img.removeAttribute('data-src');
                    };

                    imageObserver.unobserve(img);
                }
            });
        });

        lazyImages.forEach(img => {
            imageObserver.observe(img);
        });
    } else {
        // Fallback for browsers without intersection observer
        lazyImages.forEach(img => {
            img.src = img.dataset.src;
            img.removeAttribute('data-src');
        });
    }
}

// Initialize theme toggle on DOMContentLoaded
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