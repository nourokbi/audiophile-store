import React from 'react';
import { api, getSession, onSessionChange, setSession } from '../lib/api';

// Who is signed in, with login, register and logout for the pages
const AuthContext = React.createContext(null);

export function AuthProvider({ children }) {
	const [user, setUser] = React.useState(() => getSession()?.user ?? null);

	// Keeps React in step when api.js drops a refused session
	React.useEffect(() => onSessionChange((s) => setUser(s?.user ?? null)), []);

	// On first load checks that the saved session still works
	React.useEffect(() => {
		if (!getSession()) return;
		api('/auth/me').catch(() => setSession(null));
	}, []);

	const login = React.useCallback(async (email, password) => {
		setSession(await api('/auth/login', { method: 'POST', body: { email, password } }));
	}, []);

	const register = React.useCallback(async (name, email, password) => {
		setSession(await api('/auth/register', { method: 'POST', body: { name, email, password } }));
	}, []);

	const logout = React.useCallback(async () => {
		const refreshToken = getSession()?.refreshToken;
		try {
			if (refreshToken) await api('/auth/logout', { method: 'POST', body: { refreshToken } });
		} finally {
			setSession(null);
		}
	}, []);

	const value = React.useMemo(() => ({ user, login, register, logout }), [user, login, register, logout]);
	return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
	return React.useContext(AuthContext);
}
