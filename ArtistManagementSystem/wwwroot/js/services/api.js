window.API_URL = window.API_URL || 'http://localhost:5013/api';

const ApiService = {
    async request(endpoint, options = {}) {
        const url = `${window.API_URL}${endpoint}`;

        const config = {
            ...options,
            credentials: 'include',
            headers: {
                ...options.headers
            }
        };

        const response = await fetch(url, config);
        
        if (response.status === 401) {
            AuthService.logout();
            window.location.href = 'index.html';
            return { status: 401, ok: false };
        }
        
        if (response.status === 403) {
            return { status: 403, ok: false };
        }

        if (!response.ok) {
            throw new Error(`API request failed: ${response.statusText}`);
        }

        return response;
    },

    async get(endpoint) {
        return this.request(endpoint);
    },

    async post(endpoint, data, isFormData = false) {
        const headers = isFormData ? {} : { 'Content-Type': 'application/json' };
        const body = isFormData ? data : JSON.stringify(data);
        
        return this.request(endpoint, {
            method: 'POST',
            headers,
            body
        });
    },

    async put(endpoint, data) {
        return this.request(endpoint, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
    },

    async delete(endpoint) {
        return this.request(endpoint, {
            method: 'DELETE'
        });
    }
};

window.ApiService = ApiService;
