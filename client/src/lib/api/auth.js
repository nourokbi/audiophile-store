import { api } from './client';

// Login and register answer with the access token and the user, the refresh token comes as an httpOnly cookie
export const authApi = {
	login: (email, password) => api('/auth/login', { method: 'POST', body: { email, password } }),
	register: (name, email, password) => api('/auth/register', { method: 'POST', body: { name, email, password } }),
	logout: () => api('/auth/logout', { method: 'POST' }),
	me: () => api('/auth/me'),
};
