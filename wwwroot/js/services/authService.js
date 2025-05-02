// Create a new file: wwwroot/js/services/authService.js

export const AuthService = {
    getToken: () => {
        const token = document.cookie
            .split('; ')
            .find(row => row.startsWith('jwt_token='));

        const tokenValue = token ? token.split('=')[1] : null;
        console.log("Retrieved token:", tokenValue ? "[TOKEN FOUND]" : "NO TOKEN");
        return tokenValue;
    },

    getHeaders: (includeContentType = true) => {
        const token = AuthService.getToken();
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
    },

    fetchWithAuth: async (url, options = {}) => {
        const defaultOptions = {
            headers: AuthService.getHeaders(true),
            credentials: 'include'
        };

        const mergedOptions = {...defaultOptions, ...options};

        if (options.headers) {
            mergedOptions.headers = {...defaultOptions.headers, ...options.headers};
        }

        try {
            const response = await fetch(url, mergedOptions);
            return await AuthService.handleResponse(response);
        } catch (error) {
            console.error("Auth fetch error:", error);
            throw error;
        }
    },

    handleResponse: async (response) => {
        // Your existing handleApiResponse logic
    }
};