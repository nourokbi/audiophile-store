import { Navigate, Outlet, useLocation } from 'react-router-dom';

import { useAuth } from '../../context/AuthContext';

// Wraps the pages that need a signed in user, with adminOnly everyone who is not an admin goes home
const RequireAuth = ({ adminOnly = false }) => {
	const { user, isAdmin } = useAuth();
	const location = useLocation();

	if (!user) {
		const returnPath = location.pathname + location.search;
		return <Navigate to={`/login?redirect=${encodeURIComponent(returnPath)}`} replace />;
	}
	if (adminOnly && !isAdmin) return <Navigate to='/' replace />;
	return <Outlet />;
};

export default RequireAuth;
