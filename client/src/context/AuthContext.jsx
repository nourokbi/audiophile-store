import React from 'react';

import { authApi } from '../lib/api/auth';
import { onSessionChange, refreshSession, setSession } from '../lib/api/client';

const ADMIN_ROLE = 'Admin';

// Who is signed in, with login, register and logout for the pages
const AuthContext = React.createContext(null);

export function AuthProvider({ children }) {
	const [user, setUser] = React.useState(null);
	const [isRestoring, setIsRestoring] = React.useState(true);

	// Keeps React in step when the API client drops a refused session
	React.useEffect(() => onSessionChange((session) => setUser(session?.user ?? null)), []);

	// On page load the refresh cookie, if there is one, brings the session back
	React.useEffect(() => {
		refreshSession().finally(() => setIsRestoring(false));
	}, []);

	const login = React.useCallback(async (email, password) => setSession(await authApi.login(email, password)), []);

	const register = React.useCallback(
		async (name, email, password) => setSession(await authApi.register(name, email, password)),
		[],
	);

	const logout = React.useCallback(async () => {
		try {
			await authApi.logout();
		} finally {
			setSession(null);
		}
	}, []);

	const isAdmin = Boolean(user?.roles?.includes(ADMIN_ROLE));
	const value = React.useMemo(
		() => ({ user, isAdmin, isRestoring, login, register, logout }),
		[user, isAdmin, isRestoring, login, register, logout],
	);
	return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
	return React.useContext(AuthContext);
}
