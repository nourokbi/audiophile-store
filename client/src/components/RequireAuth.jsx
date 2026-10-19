import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

// Wraps the pages that need a signed in customer, anyone else goes to login and comes back afterwards
const RequireAuth = () => {
	const { user } = useAuth();
	const location = useLocation();

	if (!user) {
		const back = location.pathname + location.search;
		return <Navigate to={`/login?redirect=${encodeURIComponent(back)}`} replace />;
	}
	return <Outlet />;
};

export default RequireAuth;
