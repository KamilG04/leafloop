// Pełna ścieżka: wwwroot/js/utils/auth.js

/**
 * Gets the JWT token from cookies.
 * Looks for a cookie named 'jwt_token'.
 * @returns {string|null} JWT token or null if not found.
 */
export const getJwtToken = () => {
    try {
        const cookie = document.cookie
            .split('; ')
            .find(row => row.startsWith('jwt_token=')); // Upewnij się, że nazwa ciasteczka jest poprawna

        if (!cookie) return null;

        return cookie.split('=')[1];
    } catch (err) {
        console.error('Error getting JWT token from cookie:', err);
        return null;
    }
};

/**
 * Creates HTTP headers object including Authorization (Bearer token) if available.
 * @param {boolean} [includeContentType=true] Whether to include 'Content-Type: application/json'.
 * @returns {object} Headers object.
 */
export const getAuthHeaders = (includeContentType = true) => {
    const token = getJwtToken();
    const headers = {
        'Accept': 'application/json', // Prefer JSON responses
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
 * Checks if the user is currently considered authenticated (has a JWT token).
 * @returns {boolean} True if a JWT token exists.
 */
export const isAuthenticated = () => {
    return getJwtToken() !== null;
};

/**
 * Parses the JWT token payload to get user claims.
 * Does not verify the token signature or expiration.
 * @returns {object|null} Decoded payload object or null if token is invalid/not found.
 */
export const getUserFromToken = () => {
    const token = getJwtToken();
    if (!token) return null;

    try {
        const base64Url = token.split('.')[1]; // Get payload part
        if (!base64Url) return null;

        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            // Convert base64 decoded string to UTF-8
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    } catch (err) {
        console.error('Error parsing JWT token payload:', err);
        return null;
    }
};

/**
 * Gets the current user ID from the JWT token claims.
 * Tries common claim names for user ID.
 * @returns {number|null} User ID as a number, or null if not found or parsing fails.
 */
export const getCurrentUserId = () => {
    const userInfo = getUserFromToken();
    if (!userInfo) return null;

    // Standard ASP.NET Core Identity claim for User ID
    const nameIdentifierClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
    // Other common claims for subject/user ID
    const subClaim = 'sub';
    const nameIdClaim = 'nameid';

    const userIdStr = userInfo[nameIdentifierClaim] || userInfo[subClaim] || userInfo[nameIdClaim];

    if (userIdStr) {
        const userIdInt = parseInt(userIdStr, 10);
        return isNaN(userIdInt) ? null : userIdInt; // Return number or null if parsing failed
    }

    console.warn("Could not find User ID claim in JWT token.");
    return null;
};

/**
 * Handles a Fetch API Response object, checking for errors and parsing the body.
 * Assumes API returns JSON responses, potentially wrapped in an ApiResponse structure.
 * Handles 401 Unauthorized by redirecting to login.
 * @param {Response} response The Fetch API Response object.
 * @returns {Promise<any>} A promise that resolves with the parsed response data (or null for 204).
 * @throws {Error} Throws an error if the response status is not ok (e.g., 4xx, 5xx),
 * including a message extracted from the API response if possible.
 */
export const handleApiResponse = async (response) => {
    // Handle 401 Unauthorized - Redirect to login
    if (response.status === 401) {
        console.warn('Authentication required (401). Redirecting to login...');
        redirectToLogin(); // Use helper function
        // Throw an error to stop further processing in the calling code
        throw new Error('Authentication required');
    }

    // Handle other non-successful responses (4xx, 5xx)
    if (!response.ok) {
        let errorMessage = `API Error: ${response.status} ${response.statusText}`;
        let errorDetails = null;

        // Try to parse the error response body as JSON
        try {
            const errorData = await response.json();
            console.error("API Error Response Body:", errorData); // Log the error body

            // Check for our standard ApiResponse format
            if (errorData && typeof errorData === 'object') {
                if (errorData.message) {
                    errorMessage = errorData.message; // Use message from ApiResponse
                }
                if (errorData.errors) {
                    errorDetails = errorData.errors; // Capture validation errors or other details
                    // Optionally format validation errors into the message
                    if (typeof errorData.errors === 'object') {
                        const validationMessages = Object.values(errorData.errors).flat().join(' ');
                        if (validationMessages) {
                            errorMessage += ` Details: ${validationMessages}`;
                        }
                    }
                }
            }
        } catch (e) {
            // If parsing JSON fails or body is not JSON, log the error but stick to the status text
            console.warn("Could not parse error response body as JSON.", e);
        }

        const error = new Error(errorMessage);
        error.status = response.status; // Attach status code to error object
        error.details = errorDetails; // Attach details if available
        throw error; // Throw the error to be caught by the calling function
    }

    // Handle successful responses

    // 204 No Content - Return null as there is no body
    if (response.status === 204) {
        return null;
    }

    // For other successful responses (e.g., 200 OK, 201 Created), try to parse JSON
    try {
        // Check content type before parsing
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            const data = await response.json();
            // Return the parsed data (which might be an ApiResponse object or direct data)
            return data;
        } else {
            // If not JSON, but still successful, return true or handle based on content type
            console.warn("Received successful non-JSON response. Content-Type:", contentType);
            return true; // Indicate success without specific data
        }
    } catch (error) {
        console.error('Error parsing successful JSON response:', error);
        // If parsing fails even on success, throw an error
        throw new Error("Failed to parse successful API response.");
    }
};

// === DODANA FUNKCJA ===
/**
 * Extracts the actual data payload from a standardized ApiResponse object.
 * @param {object|any} apiResponseResult The result obtained from handleApiResponse.
 * @returns {any} The data from response.data if successful and present, otherwise null or the original result.
 */
export const getResponseData = (apiResponseResult) => {
    // Check if the result looks like our standard ApiResponse wrapper
    if (apiResponseResult && typeof apiResponseResult === 'object' &&
        apiResponseResult.hasOwnProperty('success') /* && apiResponseResult.hasOwnProperty('data') */ ) // 'data' might be missing on error
    {
        if (apiResponseResult.success) {
            // For successful responses, return the 'data' field (which could be null or the actual payload)
            return apiResponseResult.data !== undefined ? apiResponseResult.data : null;
        } else {
            // For unsuccessful responses within the wrapper (should have been caught by handleApiResponse, but as a fallback)
            console.warn("getResponseData received an unsuccessful ApiResponse object:", apiResponseResult);
            return null; // Or perhaps throw an error based on apiResponseResult.message
        }
    }

    // If the result doesn't match the ApiResponse structure, return it as is.
    // This handles cases where the API might return data directly,
    // or handleApiResponse returned null (for 204) or true (for non-JSON success).
    console.log("getResponseData received a non-standard ApiResponse object, returning as-is:", apiResponseResult);
    return apiResponseResult;
};
// === KONIEC DODANEJ FUNKCJI ===


/**
 * Redirects the user to the login page, preserving the current URL as ReturnUrl.
 */
export const redirectToLogin = () => {
    const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
    console.log(`Redirecting to login. ReturnUrl=${returnUrl}`);
    window.location.href = `/Account/Login?ReturnUrl=${returnUrl}`; // Use MVC Account controller
};

/**
 * Logs the user out by clearing the JWT cookie and redirecting to the home page.
 * Note: This does not call any backend logout endpoint.
 */
export const logout = () => {
    console.log("Logging out: Clearing jwt_token cookie and redirecting.");
    // Clear the JWT cookie
    document.cookie = 'jwt_token=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT; SameSite=Lax; Secure'; // Ensure Secure flag if using HTTPS

    // Redirect to home page
    window.location.href = '/';
};
