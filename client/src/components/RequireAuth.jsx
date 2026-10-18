import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

// Pages that need a signed-in customer (checkout, orders). Anyone else goes to /login, which
// sends them back here afterwards.
const RequireAuth = ({ children }) => {
	const { user } = useAuth();
	const location = useLocation();

	if (!user) {
		const back = location.pathname + location.search;
		return <Navigate to={`/login?redirect=${encodeURIComponent(back)}`} replace />;
	}
	return children;
};

export default RequireAuth;
