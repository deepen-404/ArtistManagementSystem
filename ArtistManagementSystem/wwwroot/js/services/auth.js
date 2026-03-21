const AuthService = {
    currentUser: null,

    init() {
        return this.currentUser;
    },

    isAuthenticated() {
        return this.currentUser !== null;
    },

    getRole() {
        return this.currentUser?.role;
    },

    isSuperAdmin() {
        return this.getRole() === 'super_admin';
    },

    isArtistManager() {
        return this.getRole() === 'artist_manager';
    },

    isArtist() {
        return this.getRole() === 'artist';
    },

    async login(email, password) {
        const response = await ApiService.post('/auth/login', { email, password });
        
        if (!response.ok) {
            throw new Error('Invalid credentials');
        }

        const data = await response.json();
        this.setSession(data);
        return data;
    },

    async register(userData) {
        const cleanData = {
            firstName: userData.firstName,
            lastName: userData.lastName,
            email: userData.email,
            password: userData.password,
            phone: userData.phone || null,
            dob: userData.dob || null,
            gender: userData.gender || null,
            address: userData.address || null,
            role: userData.role
        };
        
        const response = await ApiService.post('/auth/register', cleanData);
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Registration failed');
        }

        return response.json();
    },

    setSession(data) {
        this.currentUser = data;
    },

    logout() {
        this.currentUser = null;
    }
};

window.AuthService = AuthService;
