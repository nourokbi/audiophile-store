import React from 'react';

import { authApi } from '../lib/api/auth';
import { getSession, onSessionChange, setSession } from '../lib/api/client';

const ADMIN_ROLE = 'Admin';

// Who is signed in, with login, register and logout for the pages
const AuthContext = React.createContext(null);

export function AuthProvider({ children }) {
	const [user, setUser] = React.useState(() => getSession()?.user ?? null);

	// Keeps React in step when the API client drops a refused session
	React.useEffect(() => onSessionChange((session) => setUser(session?.user ?? null)), []);

	// On first load checks that the saved session still works
	React.useEffect(() => {
		if (!getSession()) return;
		authApi.me().catch(() => setSession(null));
	}, []);

	const login = React.useCallback(async (email, password) => setSession(await authApi.login(email, password)), []);

	const register = React.useCallback(
		async (name, email, password) => setSession(await authApi.register(name, email, password)),
		[],
	);

	const logout = React.useCallback(async () => {
		const refreshToken = getSession()?.refreshToken;
		try {
			if (refreshToken) await authApi.logout(refreshToken);
		} finally {
			setSession(null);
		}
	}, []);

	const isAdmin = Boolean(user?.roles?.includes(ADMIN_ROLE));
	const value = React.useMemo(() => ({ user, isAdmin, login, register, logout }), [user, isAdmin, login, register, logout]);
	return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
	return React.useContext(AuthContext);
}
