// wwwroot/js/utils/reactHelpers.js

/**
 * Utility functions for integrating React components with the MVC application
 */

/**
 * Safely renders a React component to a container element
 * @param {string} containerId ID of the container element
 * @param {React.ComponentType} Component React component to render
 * @param {object} props Props to pass to the component
 * @returns {ReactDOM.Root|null} Root instance or null if container not found
 */
export const renderComponent = (containerId, Component, props = {}) => {
    const container = document.getElementById(containerId);

    if (!container) {
        console.error(`Container with ID "${containerId}" not found`);
        return null;
    }

    try {
        const root = ReactDOM.createRoot(container);
        root.render(
            <React.StrictMode>
                <Component {...props} />
            </React.StrictMode>
        );
        return root;
    } catch (error) {
        console.error(`Error rendering component to "${containerId}":`, error);

        // Display a fallback error UI
        container.innerHTML = `
            <div class="alert alert-danger">
                <h4 class="alert-heading">Error Loading Component</h4>
                <p>Sorry, something went wrong while loading this component. Please try refreshing the page.</p>
                <hr>
                <p class="mb-0">Error: ${error.message}</p>
            </div>
        `;
        return null;
    }
};

/**
 * Gets attribute data from a container element
 * @param {string} containerId ID of the container element
 * @param {string} attribute The data attribute name without 'data-' prefix
 * @param {any} defaultValue Default value if attribute is not found or invalid
 * @returns {any} The attribute value or defaultValue
 */
export const getContainerAttribute = (containerId, attribute, defaultValue) => {
    const container = document.getElementById(containerId);

    if (!container) {
        console.error(`Container with ID "${containerId}" not found`);
        return defaultValue;
    }

    const attrValue = container.getAttribute(`data-${attribute}`);

    if (attrValue === null || attrValue === undefined) {
        return defaultValue;
    }

    // Try to parse as JSON if it looks like JSON
    if ((attrValue.startsWith('{') && attrValue.endsWith('}')) ||
        (attrValue.startsWith('[') && attrValue.endsWith(']'))) {
        try {
            return JSON.parse(attrValue);
        } catch {
            // If parsing fails, return as string
            return attrValue;
        }
    }

    // Try to parse as number if it looks like one
    if (/^-?\d*\.?\d+$/.test(attrValue)) {
        return Number(attrValue);
    }

    // Handle boolean values
    if (attrValue.toLowerCase() === 'true') return true;
    if (attrValue.toLowerCase() === 'false') return false;

    // Default to returning the string value
    return attrValue;
};

/**
 * Display a loading spinner in a container
 * @param {string} containerId ID of the container element
 * @param {string} message Optional loading message
 */
export const showLoading = (containerId, message = 'Loading...') => {
    const container = document.getElementById(containerId);

    if (!container) return;

    container.innerHTML = `
        <div class="d-flex justify-content-center py-5">
            <div class="spinner-border text-success" role="status" style="width: 3rem; height: 3rem;">
                <span class="visually-hidden">${message}</span>
            </div>
        </div>
    `;
};

/**
 * Display an error message in a container
 * @param {string} containerId ID of the container element
 * @param {string} message Error message to display
 * @param {boolean} isRetryable Whether to show a retry button
 * @param {Function} retryFn Function to call when retry button is clicked
 */
export const showError = (containerId, message, isRetryable = false, retryFn = null) => {
    const container = document.getElementById(containerId);

    if (!container) return;

    const retryButton = isRetryable && retryFn ?
        `<button class="btn btn-outline-danger mt-3" id="${containerId}-retry">Retry</button>` : '';

    container.innerHTML = `
        <div class="alert alert-danger" role="alert">
            <i class="bi bi-exclamation-triangle-fill me-2"></i>
            <span>${message}</span>
            <div class="text-center">
                ${retryButton}
            </div>
        </div>
    `;

    if (isRetryable && retryFn) {
        document.getElementById(`${containerId}-retry`).addEventListener('click', retryFn);
    }
};