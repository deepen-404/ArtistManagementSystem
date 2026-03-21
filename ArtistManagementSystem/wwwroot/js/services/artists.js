const ArtistService = {
    async getAll(page = 1, pageSize = 10) {
        const response = await ApiService.get(`/artists?page=${page}&pageSize=${pageSize}`);
        
        if (response.status === 403) {
            return { data: [], totalCount: 0 };
        }

        return response.json();
    },

    async getById(id) {
        const response = await ApiService.get(`/artists/${id}`);
        
        if (response.status === 403) {
            throw new Error('Access denied');
        }

        return response.json();
    },

    async create(artistData) {
        const response = await ApiService.post('/artists', artistData);
        
        if (!response.ok) {
            throw new Error('Failed to create artist');
        }

        return response.json();
    },

    async update(id, artistData) {
        const response = await ApiService.put(`/artists/${id}`, artistData);
        
        if (!response.ok) {
            throw new Error('Failed to update artist');
        }

        return response.json();
    },

    async delete(id) {
        const response = await ApiService.delete(`/artists/${id}`);
        
        if (!response.ok) {
            throw new Error('Failed to delete artist');
        }

        return true;
    },

    async importCsv(formData) {
        const response = await ApiService.post('/artists/import', formData, true);
        
        if (!response.ok) {
            throw new Error('Failed to import artists');
        }

        return response.json();
    },

    async exportCsv() {
        const response = await ApiService.get('/artists/export');
        
        if (!response.ok) {
            throw new Error('Failed to export artists');
        }

        return response.blob();
    },

    canAccess() {
        return AuthService.isArtistManager();
    },

    canCreate() {
        return AuthService.isArtistManager();
    },

    canEdit() {
        return AuthService.isArtistManager();
    },

    canDelete() {
        return AuthService.isArtistManager();
    },

    canImport() {
        return AuthService.isArtistManager();
    },

    canExport() {
        return AuthService.isArtistManager();
    }
};

window.ArtistService = ArtistService;
