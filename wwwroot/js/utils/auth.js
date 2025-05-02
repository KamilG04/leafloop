// Path: wwwroot/js/utils/auth.js

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
 * Gets the current user ID from the JWT token
 * @returns {number|null} User ID or null if not found/authenticated
 */
export const getCurrentUserId = () => {
    const token = getJwtToken();
    if (!token) return null;

    try {
        // Split the token and get the payload part
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        const decodedToken = JSON.parse(jsonPayload);

        // Check different claim types for user ID
        const nameIdClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
        const subClaim = "sub";

        // Try to get user ID from different claim types
        const userIdStr = decodedToken[nameIdClaim] || decodedToken[subClaim];
        if (!userIdStr) return null;

        // Convert to integer
        return parseInt(userIdStr, 10);
    } catch (err) {
        console.error('Error parsing JWT token:', err);
        return null;
    }
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
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/Account/Login?ReturnUrl=${returnUrl}`;
        throw new Error('Authentication required');
    }

    // For other error responses
    if (!response.ok) {
        let errorMessage = `Server error: ${response.status}`;

        try {
            // Try to get error details from response body
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                const errorData = await response.json();

                // Handle different error response formats
                if (errorData) {
                    if (typeof errorData === 'string') {
                        errorMessage = errorData;
                    } else if (errorData.message) {
                        errorMessage = errorData.message;
                    } else if (errorData.error) {
                        errorMessage = errorData.error;
                    } else if (errorData.errors) {
                        // For validation errors
                        const validationErrors = Object.values(errorData.errors)
                            .flat()
                            .join('. ');

                        if (validationErrors) {
                            errorMessage = validationErrors;
                        }
                    }
                }
            } else {
                // Try to get text response
                const textError = await response.text();
                if (textError && textError.length > 0) {
                    errorMessage = textError;
                }
            }
        } catch (err) {
            // If parsing fails, use status text
            if (response.statusText) {
                errorMessage = response.statusText;
            }
        }

        throw new Error(errorMessage);
    }

    // For successful responses with no content
    if (response.status === 204) {
        return null;
    }

    try {
        // For JSON responses
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            const data = await response.json();

            // Support for the ApiResponse wrapper format
            if (data && typeof data === 'object' && 'success' in data) {
                // If it's using our API response wrapper
                if (data.success === true) {
                    return data.data;
                } else {
                    throw new Error(data.message || 'API Error');
                }
            }

            // If not using wrapper, return the data directly
            return data;
        }

        // For created resources without JSON response
        if (response.status === 201) {
            const location = response.headers.get('Location');
            if (location) {
                const parts = location.split('/');
                const id = parseInt(parts[parts.length - 1], 10);
                if (!isNaN(id)) {
                    return { id: id };
                }
            }
        }

        // For other successful responses without JSON content
        return true;
    } catch (err) {
        if (err.message !== 'API Error') {
            console.error('Error parsing API response:', err);
        }
        throw err;
    }
};

/**
 * Redirects to login page with current URL as return URL
 */
export const redirectToLogin = () => {
    const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
    window.location.href = `/Account/Login?ReturnUrl=${returnUrl}`;
};

/**
 * Logs the user out by clearing JWT cookie and redirecting
 */
export const logout = () => {
    document.cookie = 'jwt_token=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT; SameSite=Lax; Secure';
    window.location.href = '/';
};
export const getResponseData = (apiResponseResult) => {
    // Check if the result looks like our standard ApiResponse wrapper
    if (apiResponseResult && typeof apiResponseResult === 'object' &&
        'success' in apiResponseResult)
    {
        if (apiResponseResult.success) {
            // For successful responses, return the 'data' field
            return apiResponseResult.data !== undefined ? apiResponseResult.data : null;
        } else {
            // For unsuccessful responses
            console.warn("getResponseData received an unsuccessful ApiResponse object:", apiResponseResult);
            return null;
        }
    }

    // If the result doesn't match the ApiResponse structure, return it as is
    return apiResponseResult;
};