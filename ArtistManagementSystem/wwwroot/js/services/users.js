const UserService = {
    async getAll(page = 1, pageSize = 10) {
        const response = await ApiService.get(`/users?page=${page}&pageSize=${pageSize}`);
        
        if (response.status === 403) {
            return { data: [], totalCount: 0 };
        }

        return response.json();
    },

    async getById(id) {
        const response = await ApiService.get(`/users/${id}`);
        
        if (response.status === 403) {
            throw new Error('Access denied');
        }

        return response.json();
    },

    async create(userData) {
        const response = await ApiService.post('/users', userData);
        
        if (!response.ok) {
            throw new Error('Failed to create user');
        }

        return response.json();
    },

    async update(id, userData) {
        const response = await ApiService.put(`/users/${id}`, userData);
        
        if (!response.ok) {
            throw new Error('Failed to update user');
        }

        return response.json();
    },

    async delete(id) {
        const response = await ApiService.delete(`/users/${id}`);
        
        if (!response.ok) {
            throw new Error('Failed to delete user');
        }

        return true;
    },

    canAccess() {
        return AuthService.isSuperAdmin();
    },

    canCreate() {
        return AuthService.isSuperAdmin();
    },

    canEdit() {
        return AuthService.isSuperAdmin();
    },

    canDelete() {
        return AuthService.isSuperAdmin();
    }
};

window.UserService = UserService;
