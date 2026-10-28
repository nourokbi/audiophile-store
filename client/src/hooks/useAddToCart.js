import React from 'react';

import { useCart } from '../context/CartContext';

// Add to cart for the product page and the cards, visitors and customers alike get a short notice back
export function useAddToCart() {
	const { add } = useCart();
	const [busy, setBusy] = React.useState(false);
	const [notice, setNotice] = React.useState(null); // { ok: boolean, text: string }

	// Clears the notice a few seconds after it appears
	React.useEffect(() => {
		if (!notice) return;
		const timer = setTimeout(() => setNotice(null), 3500);
		return () => clearTimeout(timer);
	}, [notice]);

	const addToCart = async (productId, quantity) => {
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
