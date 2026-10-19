import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

// Wraps the pages that need a signed in user, with role='Admin' only admins get in and everyone else goes home
const RequireAuth = ({ role }) => {
	const { user } = useAuth();
	const location = useLocation();

	if (!user) {
		const back = location.pathname + location.search;
		return <Navigate to={`/login?redirect=${encodeURIComponent(back)}`} replace />;
	}
	if (role && !user.roles?.includes(role)) return <Navigate to='/' replace />;
	return <Outlet />;
};

export default RequireAuth;
