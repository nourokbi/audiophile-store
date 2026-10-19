import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';

import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';

// Add to cart for the product page and the cards, visitors go to login first and customers get a short notice back
export function useAddToCart() {
	const { user } = useAuth();
	const { add } = useCart();
	const navigate = useNavigate();
	const location = useLocation();
	const [busy, setBusy] = React.useState(false);
	const [notice, setNotice] = React.useState(null); // { ok: boolean, text: string }

	// Clears the notice a few seconds after it appears
	React.useEffect(() => {
		if (!notice) return;
		const timer = setTimeout(() => setNotice(null), 3500);
		return () => clearTimeout(timer);
	}, [notice]);

	const addToCart = async (productId, quantity) => {
		if (!user) {
			navigate(`/login?redirect=${encodeURIComponent(location.pathname)}`);
			return false;
		}
		setBusy(true);
		try {
			await add(productId, quantity);
			setNotice({ ok: true, text: `Added ${quantity} to your cart.` });
			return true;
		} catch (e) {
			setNotice({ ok: false, text: e.message });
			return false;
		} finally {
			setBusy(false);
		}
	};

	return { addToCart, busy, notice };
}
