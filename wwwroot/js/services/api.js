class ApiService {
    static async request(url, options = {}) {
        const token = localStorage.getItem('jwt_token') ||
            document.cookie.split('; ').find(row => row.startsWith('jwt_token='))?.split('=')[1];

        const defaultOptions = {
            headers: {
                'Accept': 'application/json',
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            }
        };
        
        // Dodaj Content-Type tylko dla metod z body
        if (options.body && typeof options.body === 'string') {
            defaultOptions.headers['Content-Type'] = 'application/json';
        }

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
    // W klasie ApiService w pliku api.js
    static postFormData(url, formData) {
        // Wywołuje request, przekazując FormData bezpośrednio
        // Nie ustawia Content-Type: application/json
        return this.request(url, {
            method: 'POST',
            body: formData
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

    // Helper do poprawnej ścieżki obrazka
    static getImageUrl(path) {
        if (!path) return '/img/placeholder-item.png';
        if (path.startsWith('http://') || path.startsWith('https://')) return path;
        if (path.startsWith('/')) return path;
        return '/' + path;
    }
}

export default ApiService;