// DarkModeToggleComponent.js
import React, { useState, useEffect } from 'react';
import { getAuthHeaders, handleApiResponse } from '../utils/auth.js';

const DarkModeToggle = () => {
    const [isDarkMode, setIsDarkMode] = useState(false);
    const [loading, setLoading] = useState(true);

    // Load current theme preference on mount
    useEffect(() => {
        const fetchThemePreference = async () => {
            try {
                const response = await fetch('/api/preferences/theme', {
                    headers: getAuthHeaders(false)
                });
                const data = await handleApiResponse(response);

                // Set the initial theme based on user preference or system preference
                const userPrefersDark = data && data.theme === 'dark';
                const systemPrefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;

                setIsDarkMode(userPrefersDark || (!data && systemPrefersDark));
                applyTheme(userPrefersDark || (!data && systemPrefersDark));
            } catch (err) {
                console.error('Failed to load theme preference:', err);
                // Fall back to system preference
                const systemPrefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
                setIsDarkMode(systemPrefersDark);
                applyTheme(systemPrefersDark);
            } finally {
                setLoading(false);
            }
        };

        fetchThemePreference();
    }, []);

    // Apply theme to document
    const applyTheme = (dark) => {
        if (dark) {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
        } else {
            document.documentElement.setAttribute('data-bs-theme', 'light');
        }
    };

    // Toggle theme and save preference
    const toggleTheme = async () => {
        const newMode = !isDarkMode;
        setIsDarkMode(newMode);
        applyTheme(newMode);

        try {
            await fetch('/api/preferences/theme', {
                method: 'PUT',
                headers: getAuthHeaders(true),
                body: JSON.stringify({ theme: newMode ? 'dark' : 'light' })
            });
        } catch (err) {
            console.error('Failed to save theme preference:', err);
        }
    };

    if (loading) return null;

    return (
        <button
            className="btn btn-sm btn-link nav-link text-body"
            onClick={toggleTheme}
            aria-label={isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'}
        >
            {isDarkMode ? (
                <i className="bi bi-sun-fill" title="Light Mode"></i>
            ) : (
                <i className="bi bi-moon-fill" title="Dark Mode"></i>
            )}
        </button>
    );
};

export default DarkModeToggle;