import { api } from './client';

// Login and register answer with the session, its tokens and the user, which AuthContext saves
export const authApi = {
	login: (email, password) => api('/auth/login', { method: 'POST', body: { email, password } }),
	register: (name, email, password) => api('/auth/register', { method: 'POST', body: { name, email, password } }),
	logout: (refreshToken) => api('/auth/logout', { method: 'POST', body: { refreshToken } }),
	me: () => api('/auth/me'),
};
