const MusicService = {
    async getByArtist(artistId, page = 1, pageSize = 10) {
        const response = await ApiService.get(`/artists/${artistId}/music?page=${page}&pageSize=${pageSize}`);
        
        if (response.status === 403) {
            return { data: [], totalCount: 0 };
        }

        return response.json();
    },

    async getById(artistId, musicId) {
        const response = await ApiService.get(`/artists/${artistId}/music/${musicId}`);
        
        if (response.status === 403) {
            throw new Error('Access denied');
        }

        return response.json();
    },

    async create(artistId, musicData) {
        const response = await ApiService.post(`/artists/${artistId}/music`, musicData);
        
        if (!response.ok) {
            throw new Error('Failed to create music');
        }

        return response.json();
    },

    async update(artistId, musicId, musicData) {
        const response = await ApiService.put(`/artists/${artistId}/music/${musicId}`, musicData);
        
        if (!response.ok) {
            throw new Error('Failed to update music');
        }

        return response.json();
    },

    async delete(artistId, musicId) {
        const response = await ApiService.delete(`/artists/${artistId}/music/${musicId}`);
        
        if (!response.ok) {
            throw new Error('Failed to delete music');
        }

        return true;
    },

    canAccess() {
        return AuthService.isSuperAdmin() || AuthService.isArtistManager() || AuthService.isArtist();
    },

    canCreate() {
        return AuthService.isArtist();
    },

    canEdit() {
        return AuthService.isArtist();
    },

    canDelete() {
        return AuthService.isArtist();
    }
};

window.MusicService = MusicService;
