import React from 'react';
import { api } from '../lib/api';
import { useAuth } from './AuthContext';

// The cart lives on the server (GET /cart), so it survives a refresh and is exactly what
// checkout will order. Every cart endpoint returns the whole cart, so each action simply
// replaces what we show: no totals are calculated here.
const EMPTY = { items: [], totalQuantity: 0, subTotal: 0, shippingFee: 0, total: 0 };
const CartContext = React.createContext(null);

export function CartProvider({ children }) {
	const { user } = useAuth();
	const [cart, setCart] = React.useState(EMPTY);

	const refresh = React.useCallback(async () => {
		setCart(user ? await api('/cart') : EMPTY);
	}, [user]);

	React.useEffect(() => {
		refresh().catch(() => setCart(EMPTY));
	}, [refresh]);

	const actions = React.useMemo(
		() => ({
			add: async (productId, quantity) =>
				setCart(await api('/cart/items', { method: 'POST', body: { productId, quantity } })),
			setQuantity: async (productId, quantity) =>
				setCart(await api(`/cart/items/${productId}`, { method: 'PUT', body: { quantity } })),
			remove: async (productId) => setCart(await api(`/cart/items/${productId}`, { method: 'DELETE' })),
			clear: async () => setCart(await api('/cart', { method: 'DELETE' })),
		}),
		[],
	);

	const value = React.useMemo(() => ({ cart, refresh, ...actions }), [cart, refresh, actions]);
	return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
	return React.useContext(CartContext);
}
