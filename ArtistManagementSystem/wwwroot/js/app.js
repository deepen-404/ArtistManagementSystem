const API_URL = window.API_URL || 'http://localhost:5013/api';

let currentUser = null;

// Toast Notification System
function showToast(message, type = 'error') {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    
    const icons = {
        success: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>',
        error: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>',
        warning: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>'
    };
    
    toast.innerHTML = `${icons[type] || icons.error}<span class="toast-message">${message}</span>`;
    container.appendChild(toast);
    
    setTimeout(() => {
        toast.classList.add('hiding');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Custom Confirm Modal
function customConfirm(message) {
    return new Promise((resolve) => {
        const modal = document.getElementById('confirmModal');
        const msgEl = document.getElementById('confirmMessage');
        const cancelBtn = document.getElementById('confirmCancel');
        const okBtn = document.getElementById('confirmOk');
        const closeBtn = document.querySelector('.confirm-close');
        
        msgEl.textContent = message;
        modal.classList.remove('hidden');
        
        const cleanup = () => {
            modal.classList.add('hidden');
            cancelBtn.removeEventListener('click', onCancel);
            okBtn.removeEventListener('click', onOk);
            closeBtn.removeEventListener('click', onCancel);
        };
        
        const onCancel = () => {
            cleanup();
            resolve(false);
        };
        
        const onOk = () => {
            cleanup();
            resolve(true);
        };
        
        cancelBtn.addEventListener('click', onCancel);
        okBtn.addEventListener('click', onOk);
        closeBtn.addEventListener('click', onCancel);
    });
}

document.addEventListener('DOMContentLoaded', async () => {
    await checkAuth();
    setupEventListeners();
});

async function checkAuth() {
    try {
        const res = await fetch(`${API_URL}/auth/me`, { credentials: 'include' });
        if (res.ok) {
            const user = await res.json();
            currentUser = { id: user.id, email: user.email, firstName: user.firstName, lastName: user.lastName, role: user.role };
            showDashboard();
        } else {
            currentUser = null;
            showAuth();
        }
    } catch (err) {
        currentUser = null;
        showAuth();
    }
}

function setupEventListeners() {
    document.getElementById('loginForm').addEventListener('submit', handleLogin);
    document.getElementById('logoutBtn').addEventListener('click', handleLogout);

    // Sidebar nav
    document.querySelectorAll('.sidebar-nav-link[data-tab="users"]').forEach(btn => {
        btn.addEventListener('click', () => switchTab('users'));
    });
    document.querySelectorAll('.sidebar-nav-link[data-tab="artists"]').forEach(btn => {
        btn.addEventListener('click', () => switchTab('artists'));
    });
    document.querySelectorAll('.sidebar-nav-link[data-tab="music"]').forEach(btn => {
        btn.addEventListener('click', () => switchTab('music'));
    });

    // Legacy tab buttons support
    document.querySelectorAll('.tab-btn[data-tab="users"]').forEach(btn => {
        btn.addEventListener('click', () => switchTab('users'));
    });
    document.querySelectorAll('.tab-btn[data-tab="artists"]').forEach(btn => {
        btn.addEventListener('click', () => switchTab('artists'));
    });
    document.querySelectorAll('.tab-btn[data-tab="music"]').forEach(btn => {
        btn.addEventListener('click', () => switchTab('music'));
    });

    document.getElementById('addUserBtn')?.addEventListener('click', () => showUserModal());
    document.getElementById('usersPrevBtn')?.addEventListener('click', () => { usersPage--; loadUsers(); });
    document.getElementById('usersNextBtn')?.addEventListener('click', () => { usersPage++; loadUsers(); });

    document.getElementById('addArtistBtn')?.addEventListener('click', () => showArtistModal());
    document.getElementById('importArtistsBtn')?.addEventListener('click', () => showCsvModal());
    document.getElementById('exportArtistsBtn')?.addEventListener('click', exportArtists);
    document.getElementById('artistSelect').addEventListener('change', (e) => {
        selectedArtistId = e.target.value ? parseInt(e.target.value) : null;
        const artistName = e.target.value ? e.target.options[e.target.selectedIndex].text : 'Music';
        document.getElementById('selectedArtistName').textContent = artistName;
        loadMusic();
    });
    document.getElementById('artistsPrevBtn')?.addEventListener('click', () => { artistsPage--; loadArtists(); });
    document.getElementById('artistsNextBtn')?.addEventListener('click', () => { artistsPage++; loadArtists(); });

    document.getElementById('addMusicBtn')?.addEventListener('click', () => showMusicModal());
    document.getElementById('musicPrevBtn')?.addEventListener('click', () => { musicPage--; loadMusic(); });
    document.getElementById('musicNextBtn')?.addEventListener('click', () => { musicPage++; loadMusic(); });

    document.querySelector('.modal-close')?.addEventListener('click', hideModal);
    document.querySelector('.close-csv')?.addEventListener('click', hideCsvModal);
    document.getElementById('csvForm').addEventListener('submit', handleCsvImport);

    window.addEventListener('click', (e) => {
        if (e.target.classList.contains('modal-overlay')) {
            hideModal();
            hideCsvModal();
            document.getElementById('confirmModal')?.classList.add('hidden');
        }
    });

    // Upload area click handler
    const uploadArea = document.getElementById('uploadArea');
    const csvFileInput = document.getElementById('csvFile');
    if (uploadArea && csvFileInput) {
        uploadArea.addEventListener('click', () => csvFileInput.click());
        uploadArea.addEventListener('dragover', (e) => {
            e.preventDefault();
            uploadArea.style.borderColor = 'var(--accent)';
        });
        uploadArea.addEventListener('dragleave', () => {
            uploadArea.style.borderColor = 'var(--border)';
        });
        uploadArea.addEventListener('drop', (e) => {
            e.preventDefault();
            uploadArea.style.borderColor = 'var(--border)';
            if (e.dataTransfer.files.length) {
                csvFileInput.files = e.dataTransfer.files;
            }
        });
        csvFileInput.addEventListener('change', () => {
            if (csvFileInput.files.length) {
                uploadArea.querySelector('p').textContent = csvFileInput.files[0].name;
            }
        });
    }
}

function switchTab(tabName) {
    // Update sidebar nav
    document.querySelectorAll('.sidebar-nav-link').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.tab === tabName);
    });
    // Update legacy tabs if present
    document.querySelectorAll('.tab-btn, .auth-tab').forEach(btn => {
        if (btn.dataset.tab) {
            btn.classList.toggle('active', btn.dataset.tab === tabName);
        }
    });
    // Update tab content
    document.querySelectorAll('.tab-content').forEach(content => {
        content.classList.add('hidden');
    });
    document.getElementById(`${tabName}-tab`)?.classList.remove('hidden');
    
    // Update page title
    const titles = {
        users: 'Users',
        artists: 'Artists',
        music: 'Music'
    };
    document.getElementById('pageTitle').textContent = titles[tabName] || 'Dashboard';
    
    // Load artist dropdown when switching to music tab
    if (tabName === 'music') {
        loadArtistDropdown();
    }
}

function showAuth() {
    // Show login form
    document.getElementById('login-form').classList.remove('hidden');

    // Clear login form fields
    document.getElementById('loginEmail').value = '';
    document.getElementById('loginPassword').value = '';
    document.getElementById('loginError').textContent = '';

    // Show auth section, hide dashboard
    document.getElementById('auth-section').classList.remove('hidden');
    document.getElementById('dashboard-section').classList.add('hidden');
}

function showDashboard() {
    document.getElementById('auth-section').classList.add('hidden');
    document.getElementById('dashboard-section').classList.remove('hidden');
    initDashboard();
}

async function initDashboard() {
    if (currentUser) {
        document.getElementById('userName').textContent = `${currentUser.firstName} ${currentUser.lastName}`;
        const formattedRole = currentUser.role.replace('_', ' ').replace(/\b\w/g, l => l.toUpperCase());
        document.getElementById('userRole').textContent = formattedRole;
        document.getElementById('userRole').className = `role-badge ${currentUser.role}`;
    }
    setupPermissions();
    loadUsers();
    loadArtists();
    loadArtistDropdown();
    loadMusic();
    
    // Start on first accessible tab
    const role = currentUser?.role;
    if (role === 'super_admin') {
        switchTab('users');
    } else if (role === 'artist_manager') {
        switchTab('artists');
    } else {
        switchTab('music');
    }
}

function setupPermissions() {
    const role = currentUser?.role;
    const isSuperAdmin = role === 'super_admin';
    const isArtistManager = role === 'artist_manager';
    const isArtist = role === 'artist';

    // Users tab - only super_admin
    document.getElementById('addUserBtn')?.classList.toggle('hidden', !isSuperAdmin);
    document.querySelectorAll('.sidebar-nav-link[data-tab="users"]').forEach(btn => {
        btn.classList.toggle('hidden', !isSuperAdmin);
    });
    document.querySelectorAll('.tab-btn[data-tab="users"]').forEach(btn => {
        btn.classList.toggle('hidden', !isSuperAdmin);
    });

    // Artists tab - super_admin and artist_manager (CRUD only for artist_manager)
    document.getElementById('addArtistBtn')?.classList.toggle('hidden', !isArtistManager);
    document.getElementById('importArtistsBtn')?.classList.toggle('hidden', !isArtistManager);
    document.getElementById('exportArtistsBtn')?.classList.toggle('hidden', !isArtistManager);
    document.querySelectorAll('.sidebar-nav-link[data-tab="artists"]').forEach(btn => {
        btn.classList.toggle('hidden', !isSuperAdmin && !isArtistManager);
    });
    document.querySelectorAll('.tab-btn[data-tab="artists"]').forEach(btn => {
        btn.classList.toggle('hidden', !isSuperAdmin && !isArtistManager);
    });
    
    // Music tab - super_admin, artist_manager, artist (CRUD only for artist)
    document.getElementById('addMusicBtn')?.classList.toggle('hidden', !isArtist);
    document.querySelectorAll('.sidebar-nav-link[data-tab="music"]').forEach(btn => {
        btn.classList.toggle('hidden', !isSuperAdmin && !isArtistManager && !isArtist);
    });
    document.querySelectorAll('.tab-btn[data-tab="music"]').forEach(btn => {
        btn.classList.toggle('hidden', !isSuperAdmin && !isArtistManager && !isArtist);
    });
    document.querySelectorAll('.back-btn').forEach(btn => {
        btn.classList.toggle('hidden', !isSuperAdmin && !isArtistManager);
    });
}

async function handleLogin(e) {
    e.preventDefault();
    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;

    try {
        const res = await fetch(`${API_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ email, password })
        });

        if (!res.ok) {
            showToast('Invalid credentials');
            return;
        }

        const data = await res.json();
        currentUser = { id: data.id, email: data.email, firstName: data.firstName, lastName: data.lastName, role: data.role };
        showDashboard();
    } catch (err) {
        showToast('Login failed');
    }
}

async function handleRegister(e) {
    e.preventDefault();
    const roleSelect = document.getElementById('regRole');
    const request = {
        firstName: document.getElementById('regFirstName').value,
        lastName: document.getElementById('regLastName').value,
        email: document.getElementById('regEmail').value,
        password: document.getElementById('regPassword').value,
        phone: document.getElementById('regPhone').value || null,
        dob: (() => { const d = document.getElementById('regDob').value; return d ? new Date(d + 'T00:00:00').toISOString() : null; })(),
        gender: document.getElementById('regGender').value || null,
        address: document.getElementById('regAddress').value || null,
        role: roleSelect.value
    };

    try {
        const res = await fetch(`${API_URL}/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(request)
        });

        if (!res.ok) {
            const err = await res.json();
            showToast(err.error || 'Registration failed');
            return;
        }

        showToast('Registration successful! Please login.', 'success');
        document.querySelector('[data-tab="login"]').click();
    } catch (err) {
        showToast('Registration failed');
    }
}

async function handleLogout() {
    try {
        await fetch(`${API_URL}/auth/logout`, {
            method: 'POST',
            credentials: 'include'
        });
    } catch (err) {}
    
    currentUser = null;
    showAuth();
}

async function loadUsers() {
    if (currentUser?.role !== 'super_admin') return;
    document.getElementById('usersTableBody').innerHTML = '<tr><td colspan="9" style="text-align: center; padding-block: 17rem">Loading...</td></tr>';
    try {
        const res = await fetch(`${API_URL}/users?page=${usersPage}&pageSize=${pageSize}`, { credentials: 'include' });
        if (res.status === 401) {
            currentUser = null;
            showAuth();
            return;
        }
        if (res.status === 403) return;
        const data = await res.json();
        renderUsers(data.data);
        document.getElementById('usersPageInfo').textContent = `Page ${usersPage}`;
        document.getElementById('usersTotalCount').textContent = data.totalCount ? `(${data.totalCount})` : '';
        const totalPages = Math.ceil(data.totalCount / pageSize);
        document.getElementById('usersPrevBtn').disabled = usersPage <= 1;
        document.getElementById('usersNextBtn').disabled = usersPage >= totalPages || totalPages === 0;
    } catch (err) {
        console.error('Failed to load users');
    }
}

function renderUsers(users) {
    const tbody = document.getElementById('usersTableBody');
    if (!users || users.length === 0) {
        tbody.innerHTML = '<tr><td colspan="9" style="text-align: center; padding-block: 17rem">No data found</td></tr>';
        return;
    }
    tbody.innerHTML = users.map(user => `
        <tr>
            <td>${user.id}</td>
            <td>${user.firstName} ${user.lastName}</td>
            <td>${user.email}</td>
            <td>${user.phone || '-'}</td>
            <td>${user.dob ? new Date(user.dob).toLocaleDateString() : '-'}</td>
            <td>${user.gender ? `<span class="gender-badge ${user.gender}">${user.gender === 'm' ? 'Male' : user.gender === 'f' ? 'Female' : 'Other'}</span>` : '-'}</td>
            <td>${user.address || '-'}</td>
            <td><span class="role-badge ${user.role}">${user.role.replace('_', ' ')}</span></td>
            <td class="action-buttons">
                <button class="btn-success" onclick="editUser(${user.id})">Edit</button>
                <button class="btn-danger" onclick="deleteUser(${user.id})">Delete</button>
            </td>
        </tr>
    `).join('');
}

async function editUser(id) {
    try {
        const res = await fetch(`${API_URL}/users/${id}`, { credentials: 'include' });
        const user = await res.json();
        showUserModal(user);
    } catch (err) {
        showToast('Failed to load user');
    }
}

async function deleteUser(id) {
    if (!await customConfirm('Are you sure you want to delete this user?')) return;
    try {
        const res = await fetch(`${API_URL}/users/${id}`, { method: 'DELETE', credentials: 'include' });
        if (res.ok) {
            loadUsers();
            showToast('User deleted successfully', 'success');
        } else {
            showToast('Failed to delete user');
        }
    } catch (err) {
        showToast('Failed to delete user');
    }
}

function showUserModal(user = null) {
    const isEdit = !!user;
    document.getElementById('modalTitle').textContent = isEdit ? 'Edit User' : 'Add User';
    document.getElementById('modalForm').innerHTML = `
        <div class="form-group"><label>First Name <span class="required">*</span></label><input type="text" name="firstName" value="${user?.firstName || ''}" required></div>
        <div class="form-group"><label>Last Name <span class="required">*</span></label><input type="text" name="lastName" value="${user?.lastName || ''}" required></div>
        <div class="form-group"><label>Email <span class="required">*</span></label><input type="email" name="email" value="${user?.email || ''}" required></div>
        ${!isEdit ? '<div class="form-group"><label>Password <span class="required">*</span></label><input type="password" name="password" required></div>' : ''}
        <div class="form-group"><label>Phone</label><input type="text" name="phone" value="${user?.phone || ''}"></div>
        <div class="form-group"><label>Date of Birth</label><input type="date" name="dob" value="${user?.dob ? user.dob.split('T')[0] : ''}"></div>
        <div class="form-group"><label>Gender</label>
            <select name="gender">
                <option value="">Select Gender</option>
                <option value="m" ${user?.gender === 'm' ? 'selected' : ''}>Male</option>
                <option value="f" ${user?.gender === 'f' ? 'selected' : ''}>Female</option>
                <option value="o" ${user?.gender === 'o' ? 'selected' : ''}>Other</option>
            </select>
        </div>
        <div class="form-group"><label>Address</label><input type="text" name="address" value="${user?.address || ''}"></div>
        <div class="form-group"><label>Role</label>
            <select name="role">
                <option value="artist" ${user?.role === 'artist' ? 'selected' : ''}>Artist</option>
                <option value="artist_manager" ${user?.role === 'artist_manager' ? 'selected' : ''}>Artist Manager</option>
                <option value="super_admin" ${user?.role === 'super_admin' ? 'selected' : ''}>Super Admin</option>
            </select>
        </div>
        <button type="submit" class="btn-primary">${isEdit ? 'Update' : 'Create'}</button>
    `;
    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const formData = new FormData(e.target);
        const data = Object.fromEntries(formData);
        if (!isEdit) data.password = formData.get('password');
        if (data.dob) data.dob = new Date(data.dob).toISOString();
        else data.dob = null;
        
        data.gender = data.gender || null;
        data.phone = data.phone || null;
        data.address = data.address || null;
        
        const url = isEdit ? `${API_URL}/users/${user.id}` : `${API_URL}/users`;
        const method = isEdit ? 'PUT' : 'POST';
        
        const res = await fetch(url, { method, headers: {'Content-Type': 'application/json'}, credentials: 'include', body: JSON.stringify(data) });
        if (res.ok) { 
            hideModal(); 
            loadUsers(); 
            showToast(isEdit ? 'User updated successfully' : 'User created successfully', 'success');
        } else {
            const err = await res.json();
            showToast(err.error || 'Failed to save user');
        }
    };
    document.getElementById('modal').classList.remove('hidden');
}

async function loadArtists() {
    if (!['super_admin', 'artist_manager'].includes(currentUser?.role)) {
        document.getElementById('artistsTableBody').innerHTML = '<tr><td colspan="8" style="text-align: center; padding-block: 17rem">Access denied</td></tr>';
        return;
    }
    document.getElementById('artistsTableBody').innerHTML = '<tr><td colspan="8" style="text-align: center; padding-block: 17rem">Loading...</td></tr>';
    try {
        const res = await fetch(`${API_URL}/artists?page=${artistsPage}&pageSize=${pageSize}`, { credentials: 'include' });
        if (res.status === 401) {
            currentUser = null;
            showAuth();
            return;
        }
        if (res.status === 403) return;
        const data = await res.json();
        renderArtists(data.data);
        document.getElementById('artistsPageInfo').textContent = `Page ${artistsPage}`;
        document.getElementById('artistsTotalCount').textContent = data.totalCount ? `(${data.totalCount})` : '';
        const totalPages = Math.ceil(data.totalCount / pageSize);
        document.getElementById('artistsPrevBtn').disabled = artistsPage <= 1;
        document.getElementById('artistsNextBtn').disabled = artistsPage >= totalPages || totalPages === 0;
    } catch (err) {
        console.error('Failed to load artists');
    }
}

function renderArtists(artists) {
    const tbody = document.getElementById('artistsTableBody');
    const canEdit = currentUser?.role === 'artist_manager';
    
    if (!artists || artists.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" style="text-align: center; padding-block: 17rem">No data found</td></tr>';
        return;
    }

    tbody.innerHTML = artists.map(a => `
        <tr>
            <td>${a.id}</td>
            <td>${a.name}</td>
            <td>${a.dob ? new Date(a.dob).toLocaleDateString() : '-'}</td>
            <td>${a.gender ? `<span class="gender-badge ${a.gender}">${a.gender === 'm' ? 'Male' : a.gender === 'f' ? 'Female' : 'Other'}</span>` : '-'}</td>
            <td>${a.address || '-'}</td>
            <td>${a.firstReleaseYear}</td>
            <td>${a.noOfAlbumsReleased}</td>
            <td class="action-buttons">
                ${canEdit ? `<button class="btn-success" onclick="editArtist(${a.id})">Edit</button>
                <button class="btn-danger" onclick="deleteArtist(${a.id})">Delete</button>` : ''}
                <button class="btn-secondary" onclick="viewArtistMusic(${a.id})">Music</button>
            </td>
        </tr>
    `).join('');
}

async function editArtist(id) {
    try {
        const res = await fetch(`${API_URL}/artists/${id}`, { credentials: 'include' });
        const artist = await res.json();
        showArtistModal(artist);
    } catch (err) {
        showToast('Failed to load artist');
    }
}

async function deleteArtist(id) {
    if (!await customConfirm('Are you sure you want to delete this artist?')) return;
    try {
        const res = await fetch(`${API_URL}/artists/${id}`, { method: 'DELETE', credentials: 'include' });
        if (res.ok) {
            loadArtists();
            loadArtistDropdown();
            showToast('Artist deleted successfully', 'success');
        } else {
            showToast('Failed to delete artist');
        }
    } catch (err) {
        showToast('Failed to delete artist');
    }
}

function showArtistModal(artist = null) {
    const isEdit = !!artist;
    document.getElementById('modalTitle').textContent = isEdit ? 'Edit Artist' : 'Add Artist';
    document.getElementById('modalForm').innerHTML = `
        <div class="form-group"><label>Name <span class="required">*</span></label><input type="text" name="name" value="${artist?.name || ''}" required></div>
        <div class="form-group"><label>Date of Birth</label><input type="date" name="dob" value="${artist?.dob ? artist.dob.split('T')[0] : ''}"></div>
        <div class="form-group"><label>Gender</label>
            <select name="gender">
                <option value="">Select Gender</option>
                <option value="m" ${artist?.gender === 'm' ? 'selected' : ''}>Male</option>
                <option value="f" ${artist?.gender === 'f' ? 'selected' : ''}>Female</option>
                <option value="o" ${artist?.gender === 'o' ? 'selected' : ''}>Other</option>
            </select>
        </div>
        <div class="form-group"><label>Address</label><input type="text" name="address" value="${artist?.address || ''}"></div>
        <div class="form-group"><label>First Release Year <span class="required">*</span></label><input type="number" name="firstReleaseYear" value="${artist?.firstReleaseYear || ''}" required></div>
        <div class="form-group"><label>Albums Released</label><input type="number" name="noOfAlbumsReleased" value="${artist?.noOfAlbumsReleased || 0}"></div>
        <button type="submit" style="width: 100%;" class="btn-primary">${isEdit ? 'Update' : 'Create'}</button>
    `;
    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const formData = new FormData(e.target);
        const data = Object.fromEntries(formData);
        data.firstReleaseYear = parseInt(data.firstReleaseYear);
        data.noOfAlbumsReleased = parseInt(data.noOfAlbumsReleased || 0);
        if (data.dob) data.dob = new Date(data.dob).toISOString();
        
        const url = artist ? `${API_URL}/artists/${artist.id}` : `${API_URL}/artists`;
        const method = artist ? 'PUT' : 'POST';
        
        const res = await fetch(url, { method, headers: {'Content-Type': 'application/json'}, credentials: 'include', body: JSON.stringify(data) });
        if (res.ok) { 
            hideModal(); 
            loadArtists(); 
            loadArtistDropdown();
            showToast(isEdit ? 'Artist updated successfully' : 'Artist created successfully', 'success');
        } else {
            const err = await res.json();
            showToast(err.error || 'Failed to save artist');
        }
    };
    document.getElementById('modal').classList.remove('hidden');
}

async function loadArtistDropdown() {
    if (!['super_admin', 'artist_manager', 'artist'].includes(currentUser?.role)) return;
    const res = await fetch(`${API_URL}/artists?page=1&pageSize=1000`, { credentials: 'include' });
    if (!res.ok) return;
    const data = await res.json();
    const select = document.getElementById('artistSelect');
    const currentValue = select.value;
    select.innerHTML = '<option value="">Select Artist</option>' + data.data.map(a => `<option value="${a.id}">${a.name}</option>`).join('');
    if (currentValue) {
        select.value = currentValue;
    }
}

function viewArtistMusic(artistId) {
    selectedArtistId = artistId;
    document.getElementById('artistSelect').value = artistId;
    const artistName = document.querySelector(`#artistSelect option[value="${artistId}"]`)?.textContent || 'Music';
    document.getElementById('selectedArtistName').textContent = artistName;
    switchTab('music');
    loadMusic();
}

async function exportArtists() {
    const res = await fetch(`${API_URL}/artists/export`, { credentials: 'include' });
    const blob = await res.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'artists.csv';
    a.click();
    window.URL.revokeObjectURL(url);
    showToast('Artists exported successfully', 'success');
}

function showCsvModal() {
    document.getElementById('csvModal').classList.remove('hidden');
}

function hideCsvModal() {
    document.getElementById('csvModal').classList.add('hidden');
    document.getElementById('csvFile').value = '';
    document.getElementById('uploadArea').querySelector('p').textContent = 'Drop your CSV file here or click to browse';
}

async function handleCsvImport(e) {
    e.preventDefault();
    const fileInput = document.getElementById('csvFile');
    const file = fileInput.files[0];
    
    if (!file) {
        showToast('Please select a file first');
        return;
    }
    
    if (!file.name.toLowerCase().endsWith('.csv')) {
        showToast('Only CSV files are allowed');
        return;
    }
    
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetch(`${API_URL}/artists/import`, { method: 'POST', credentials: 'include', body: formData });
    const data = await res.json();
    
    if (res.ok) {
        fileInput.value = '';
        document.getElementById('uploadArea').querySelector('p').textContent = 'Drop your CSV file here or click to browse';
        hideCsvModal();
        loadArtists();
        loadArtistDropdown();
        const imported = data.importedCount || 0;
        const skipped = data.skippedCount || 0;
        showToast(`Imported ${imported} artist(s), skipped ${skipped}`, 'success');
    } else {
        showToast(data.error || 'Import failed');
    }
}

async function loadMusic() {
    if (!selectedArtistId) {
        document.getElementById('musicTableBody').innerHTML = '<tr><td colspan="6" style="text-align: center; padding-block: 17rem">Select an artist to view their music.</td></tr>';
        document.getElementById('musicPrevBtn').disabled = true;
        document.getElementById('musicNextBtn').disabled = true;
        document.getElementById('musicPageInfo').textContent = 'Page 1';
        return;
    }
    document.getElementById('musicTableBody').innerHTML = '<tr><td colspan="6" style="text-align: center; padding-block: 17rem">Loading...</td></tr>';
    try {
        const res = await fetch(`${API_URL}/artists/${selectedArtistId}/music?page=${musicPage}&pageSize=${pageSize}`, { credentials: 'include' });
        if (res.status === 401) {
            currentUser = null;
            showAuth();
            return;
        }
        if (res.status === 403) {
            document.getElementById('musicTableBody').innerHTML = '<tr><td colspan="6" style="text-align: center; padding-block: 17rem">Access denied</td></tr>';
            return;
        }
        const data = await res.json();
        renderMusic(data.data);
        document.getElementById('musicPageInfo').textContent = `Page ${musicPage}`;
        document.getElementById('musicTotalCount').textContent = data.totalCount ? `(${data.totalCount})` : '';
        const totalPages = Math.ceil(data.totalCount / pageSize);
        document.getElementById('musicPrevBtn').disabled = musicPage <= 1;
        document.getElementById('musicNextBtn').disabled = musicPage >= totalPages || totalPages === 0;
    } catch (err) {
        console.error('Failed to load music');
    }
}

function renderMusic(musicList) {
    const tbody = document.getElementById('musicTableBody');
    const canEdit = currentUser?.role === 'artist';
    
    if (!musicList || musicList.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding-block: 17rem">No data found</td></tr>';
        return;
    }
    
    tbody.innerHTML = musicList.map(m => `
        <tr>
            <td>${m.id}</td>
            <td>${m.title}</td>
            <td>${m.albumName || '-'}</td>
            <td><span class="genre-badge ${m.genre}">${m.genre}</span></td>
            <td>${new Date(m.createdAt).toLocaleDateString()}</td>
            <td class="action-buttons">
                ${canEdit ? `<button class="btn-success" onclick="editMusic(${m.id})">Edit</button>
                <button class="btn-danger" onclick="deleteMusic(${m.id})">Delete</button>` : '-'}
            </td>
        </tr>
    `).join('');
}

function showMusicModal(music = null) {
    if (!selectedArtistId) { showToast('Please select an artist'); return; }
    const isEdit = !!music;
    document.getElementById('modalTitle').textContent = isEdit ? 'Edit Music' : 'Add Music';
    document.getElementById('modalForm').innerHTML = `
        <div class="form-group"><label>Title <span class="required">*</span></label><input type="text" name="title" value="${music?.title || ''}" required></div>
        <div class="form-group"><label>Album Name</label><input type="text" name="albumName" value="${music?.albumName || ''}"></div>
        <div class="form-group"><label>Genre <span class="required">*</span></label>
            <select name="genre" required>
                <option value="rnb" ${music?.genre === 'rnb' ? 'selected' : ''}>RNB</option>
                <option value="country" ${music?.genre === 'country' ? 'selected' : ''}>Country</option>
                <option value="classic" ${music?.genre === 'classic' ? 'selected' : ''}>Classic</option>
                <option value="rock" ${music?.genre === 'rock' ? 'selected' : ''}>Rock</option>
                <option value="jazz" ${music?.genre === 'jazz' ? 'selected' : ''}>Jazz</option>
            </select>
        </div>
        <button type="submit" class="btn-primary">${isEdit ? 'Update' : 'Create'}</button>
    `;
    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const formData = new FormData(e.target);
        const data = Object.fromEntries(formData);
        const url = music ? `${API_URL}/artists/${selectedArtistId}/music/${music.id}` : `${API_URL}/artists/${selectedArtistId}/music`;
        const method = music ? 'PUT' : 'POST';
        const res = await fetch(url, { method, headers: {'Content-Type': 'application/json'}, credentials: 'include', body: JSON.stringify(data) });
        if (res.ok) {
            hideModal();
            loadMusic();
            showToast(isEdit ? 'Music updated successfully' : 'Music created successfully', 'success');
        } else {
            const err = await res.json();
            showToast(err.error || 'Failed to save music');
        }
    };
    document.getElementById('modal').classList.remove('hidden');
}

async function editMusic(id) {
    const res = await fetch(`${API_URL}/artists/${selectedArtistId}/music/${id}`, { credentials: 'include' });
    const music = await res.json();
    showMusicModal(music);
}

async function deleteMusic(id) {
    if (!await customConfirm('Are you sure you want to delete this music?')) return;
    const res = await fetch(`${API_URL}/artists/${selectedArtistId}/music/${id}`, { method: 'DELETE', credentials: 'include' });
    if (res.ok) {
        loadMusic();
        showToast('Music deleted successfully', 'success');
    } else {
        showToast('Failed to delete music');
    }
}

function hideModal() {
    document.getElementById('modal').classList.add('hidden');
}

let usersPage = 1;
let artistsPage = 1;
let musicPage = 1;
const pageSize = 10;
let selectedArtistId = null;
