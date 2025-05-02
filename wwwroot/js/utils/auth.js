// wwwroot/js/utils/auth.js

/**
 * Gets the JWT token from cookies
 * @returns {string|null} JWT token or null if not found
 */
export const getJwtToken = () => {
    try {
        const cookie = document.cookie
            .split('; ')
            .find(row => row.startsWith('jwt_token='));

        if (!cookie) return null;

        return cookie.split('=')[1];
    } catch (err) {
        console.error('Error getting JWT token from cookie:', err);
        return null;
    }
};

/**
 * Creates HTTP headers object including authorization if available
 * @param {boolean} [includeContentType=true] Whether to include Content-Type header
 * @returns {object} Headers object
 */
export const getAuthHeaders = (includeContentType = true) => {
    const token = getJwtToken();
    const headers = {
        'Accept': 'application/json',
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    if (includeContentType) {
        headers['Content-Type'] = 'application/json';
    }

    return headers;
};

/**
 * Check if the user is currently authenticated
 * @returns {boolean} True if authenticated
 */
export const isAuthenticated = () => {
    return getJwtToken() !== null;
};

/**
 * Parses the JWT token to get the user claims
 * @returns {object|null} User claims or null if invalid/not found
 */
export const getUserFromToken = () => {
    const token = getJwtToken();
    if (!token) return null;

    try {
        // Get the payload part of the token
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    } catch (err) {
        console.error('Error parsing JWT token:', err);
        return null;
    }
};

/**
 * Gets the current user ID from the token
 * @returns {number|null} User ID or null if not found/authenticated
 */
export const getCurrentUserId = () => {
    const userInfo = getUserFromToken();
    if (!userInfo) return null;

    // Try different claim types (depends on your JWT setup)
    const nameIdentifierClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
    const subClaim = 'sub';

    if (userInfo[nameIdentifierClaim]) {
        return parseInt(userInfo[nameIdentifierClaim], 10);
    } else if (userInfo[subClaim]) {
        return parseInt(userInfo[subClaim], 10);
    }

    return null;
};

/**
 * Handles API response, checks for errors, parses JSON
 * @param {Response} response Fetch API Response object
 * @returns {Promise<any>} Parsed response data
 * @throws {Error} If response has an error status
 */
export const handleApiResponse = async (response) => {
    // Handle authentication errors
    if (response.status === 401) {
        console.warn('Authentication required. Redirecting to login...');
        // Save current URL to return after login
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/Account/Login?ReturnUrl=${returnUrl}`;
        throw new Error('Authentication required');
    }

    // For other error responses, try to extract error details
    if (!response.ok) {
        let errorMessage = `Server error: ${response.status}`;

        try {
            // Try to get error details from response body
            const errorData = await response.json();

            // Handle standard API error format
            if (errorData && !errorData.success && errorData.message) {
                errorMessage = errorData.message;
            }
            // Handle validation errors
            else if (errorData.errors) {
                const validationErrors = Object.values(errorData.errors)
                    .flat()
                    .join('. ');

                errorMessage = validationErrors || errorMessage;
            }
            // Handle simple string error
            else if (typeof errorData === 'string') {
                errorMessage = errorData;
            }
        } catch {
            // If parsing JSON fails, use the status text
            errorMessage = response.statusText || errorMessage;
        }

        throw new Error(errorMessage);
    }

    // For successful responses with no content
    if (response.status === 204) {
        return null;
    }

    // For successful responses with content
    try {
        // Check if content type is JSON
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            return await response.json();
        }

        // For non-JSON responses, return true to indicate success
        return true;
    } catch (error) {
        console.warn('Error parsing JSON response:', error);

        // For created resources (201), try to extract ID from Location header
        if (response.status === 201) {
            const location = response.headers.get('Location');
            if (location) {
                const parts = location.split('/');
                const id = parseInt(parts[parts.length - 1], 10);
                if (!isNaN(id)) {
                    return { id };
                }
            }
        }

        // Default to true to indicate success
        return true;
    }
};

/**
 * Redirects to login page
 */
export const redirectToLogin = () => {
    const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
    window.location.href = `/Account/Login?ReturnUrl=${returnUrl}`;
};

/**
 * Logs the user out
 */
export const logout = () => {
    // Clear the JWT cookie
    document.cookie = 'jwt_token=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT; SameSite=Lax; Secure';

    // Redirect to home page
    window.location.href = '/';
};