// Simple API service for making authenticated requests
class ApiService {
    static async request(url, options = {}) {
        const token = localStorage.getItem('jwt_token') ||
            document.cookie.split('; ').find(row => row.startsWith('jwt_token='))?.split('=')[1];

        const defaultOptions = {
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            }
        };

        const finalOptions = {
            ...defaultOptions,
            ...options,
            headers: {
                ...defaultOptions.headers,
                ...options.headers
            }
        };

        try {
            const response = await fetch(url, finalOptions);

            if (response.status === 401) {
                window.location.href = '/Account/Login';
                throw new Error('Authentication required');
            }

            if (!response.ok) {
                const error = await response.json().catch(() => null);
                throw new Error(error?.message || `HTTP error! status: ${response.status}`);
            }

            if (response.status === 204) {
                return null;
            }

            const data = await response.json();

            // Handle API response format
            if (data && typeof data === 'object' && 'success' in data) {
                if (data.success) {
                    return data.data;
                } else {
                    throw new Error(data.message || 'API request failed');
                }
            }

            return data;
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    }

    static get(url) {
        return this.request(url, { method: 'GET' });
    }

    static post(url, data) {
        return this.request(url, {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    static put(url, data) {
        return this.request(url, {
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    static delete(url) {
        return this.request(url, { method: 'DELETE' });
    }
}

export default ApiService;