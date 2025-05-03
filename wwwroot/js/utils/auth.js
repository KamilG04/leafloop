// Pełna ścieżka: wwwroot/js/utils/auth.js

/**
 * Gets the JWT token from cookies.
 * Looks for a cookie named 'jwt_token'.
 * @returns {string|null} JWT token or null if not found.
 */
export const getJwtToken = () => {
    console.log("getJwtToken: Checking multiple sources for token...");

    // Try localStorage first (more reliable across browsers)
    try {
        const localToken = localStorage.getItem('jwt_token');
        if (localToken) {
            console.log("getJwtToken: Found token in localStorage");
            return localToken;
        }
    } catch (err) {
        console.warn("getJwtToken: Error accessing localStorage:", err);
    }

    // Try cookies next
    try {
        const cookieValue = document.cookie;
        console.log("getJwtToken: Raw cookies:", cookieValue);

        const cookie = cookieValue
            .split('; ')
            .find(row => row.startsWith('jwt_token='));

        if (cookie) {
            const token = cookie.split('=')[1];
            console.log("getJwtToken: Found token in cookies");
            return token;
        }
    } catch (err) {
        console.warn("getJwtToken: Error accessing cookies:", err);
    }

    // No token found
    console.warn("getJwtToken: No token found in any storage source");
    return null;
};
/**
 * Creates HTTP headers object including Authorization (Bearer token) if available.
 * @param {boolean} [includeContentType=true] Whether to include 'Content-Type: application/json'.
 * @returns {object} Headers object.
 */
export const getAuthHeaders = (includeContentType = true) => {
    console.log("getAuthHeaders: Creating headers...");
    const token = getJwtToken(); // Wywołuje logowanie z getJwtToken
    const headers = {
        'Accept': 'application/json',
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
        console.log("getAuthHeaders: Authorization header ADDED.");
    } else {
        console.warn("getAuthHeaders: Authorization header NOT ADDED (no token found).");
    }

    if (includeContentType) {
        headers['Content-Type'] = 'application/json';
        console.log("getAuthHeaders: Content-Type header ADDED.");
    } else {
        console.log("getAuthHeaders: Content-Type header SKIPPED.");
    }
    console.log("getAuthHeaders: Final headers object:", headers);
    return headers;
};

// Funkcje isAuthenticated, getUserFromToken, getCurrentUserId (bez zmian, używają getJwtToken z logowaniem)

export const isAuthenticated = () => {
    return getJwtToken() !== null;
};

export const getUserFromToken = () => {
    const token = getJwtToken();
    if (!token) return null;
    try {
        const base64Url = token.split('.')[1];
        if (!base64Url) return null;
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch (err) {
        console.error('Error parsing JWT token payload:', err);
        return null;
    }
};

export const getCurrentUserId = () => {
    const userInfo = getUserFromToken();
    if (!userInfo) return null;
    const nameIdentifierClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
    const subClaim = 'sub';
    const nameIdClaim = 'nameid';
    const userIdStr = userInfo[nameIdentifierClaim] || userInfo[subClaim] || userInfo[nameIdClaim];
    if (userIdStr) {
        const userIdInt = parseInt(userIdStr, 10);
        return isNaN(userIdInt) ? null : userIdInt;
    }
    console.warn("Could not find User ID claim in JWT token.");
    return null;
};


// Funkcja handleApiResponse (bez zmian, ale jej logowanie błędów jest ważne)
export const handleApiResponse = async (response) => {
    if (response.status === 401) {
        console.warn('handleApiResponse: Authentication required (401). Redirecting to login...');
        redirectToLogin();
        throw new Error('Authentication required');
    }
    if (response.status === 403) {
        console.warn('handleApiResponse: Access Denied (403).');
        // Można przekierować do AccessDenied lub rzucić błąd
        // redirectToLogin(); // Prawdopodobnie nie chcemy przekierowywać do logowania przy 403
        throw new Error('Access Denied. You do not have permission.');
    }

    if (!response.ok) {
        let errorMessage = `API Error: ${response.status} ${response.statusText || ''}`;
        let errorDetails = null;
        try {
            const errorData = await response.json();
            console.error("handleApiResponse: API Error Response Body:", errorData);
            if (errorData && typeof errorData === 'object') {
                if (errorData.message) errorMessage = errorData.message;
                if (errorData.errors) errorDetails = errorData.errors;
                if (errorDetails && typeof errorDetails === 'object') {
                    const validationMessages = Object.values(errorDetails).flat().join(' ');
                    if (validationMessages) errorMessage += ` Details: ${validationMessages}`;
                }
            }
        } catch (e) {
            console.warn("handleApiResponse: Could not parse error response body as JSON.", e);
        }
        const error = new Error(errorMessage);
        error.status = response.status;
        error.details = errorDetails;
        console.error("handleApiResponse: Throwing error:", error);
        throw error;
    }

    if (response.status === 204) {
        console.log("handleApiResponse: Received 204 No Content.");
        return null;
    }

    try {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            const data = await response.json();
            console.log("handleApiResponse: Parsed JSON response:", data);
            return data; // Zwraca sparsowane dane (mogą być opakowane w ApiResponse)
        } else {
            console.log("handleApiResponse: Received successful non-JSON response.");
            return true;
        }
    } catch (error) {
        console.error('handleApiResponse: Error parsing successful JSON response:', error);
        throw new Error("Failed to parse successful API response.");
    }
};

// Funkcja getResponseData (bez zmian, ale jej logowanie jest ważne)
export const getResponseData = (apiResponseResult) => {
    console.log("getResponseData: Input:", apiResponseResult);
    if (apiResponseResult && typeof apiResponseResult === 'object' &&
        apiResponseResult.hasOwnProperty('success'))
    {
        if (apiResponseResult.success) {
            const data = apiResponseResult.data !== undefined ? apiResponseResult.data : null;
            console.log("getResponseData: Extracted data from successful ApiResponse:", data);
            return data;
        } else {
            console.warn("getResponseData: Received unsuccessful ApiResponse object:", apiResponseResult);
            return null;
        }
    }
    console.log("getResponseData: Input was not standard ApiResponse, returning as-is.");
    return apiResponseResult;
};


// Funkcje redirectToLogin, logout (bez zmian)
export const redirectToLogin = () => {
    const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
    console.log(`redirectToLogin: Redirecting to login. ReturnUrl=${returnUrl}`);
    window.location.href = `/Account/Login?ReturnUrl=${returnUrl}`;
};

export const logout = () => {
    console.log("logout: Clearing jwt_token cookie and redirecting.");
    document.cookie = 'jwt_token=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT; SameSite=Lax; Secure';
    localStorage.removeItem('jwt_token'); // Usuń też z localStorage
    window.location.href = '/';
};
