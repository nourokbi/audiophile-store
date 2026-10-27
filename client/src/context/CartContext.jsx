import React from 'react';

import { useAuth } from './AuthContext';
import { cartApi } from '../lib/api/cart';

// The cart lives on the server and every cart endpoint returns the whole cart, so each action just replaces what we show
const EMPTY_CART = { items: [], totalQuantity: 0, subTotal: 0, shippingFee: 0, total: 0 };
const CartContext = React.createContext(null);

export function CartProvider({ children }) {
	const { user } = useAuth();
	const [cart, setCart] = React.useState(EMPTY_CART);

	const refresh = React.useCallback(async () => {
		setCart(user ? await cartApi.get() : EMPTY_CART);
	}, [user]);

	React.useEffect(() => {
		refresh().catch(() => setCart(EMPTY_CART));
	}, [refresh]);

	const actions = React.useMemo(
		() => ({
			add: async (productId, quantity) => setCart(await cartApi.addItem(productId, quantity)),
			setQuantity: async (productId, quantity) => setCart(await cartApi.setQuantity(productId, quantity)),
			remove: async (productId) => setCart(await cartApi.removeItem(productId)),
			clear: async () => setCart(await cartApi.clear()),
		}),
		[],
	);

	const value = React.useMemo(() => ({ cart, refresh, ...actions }), [cart, refresh, actions]);
	return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
	return React.useContext(CartContext);
}
