import React from 'react';

import { useAuth } from './AuthContext';
import { cartApi } from '../lib/api/cart';
import { readGuestCart, saveGuestCart, withQuantity } from '../lib/guestCart';

// Every cart endpoint returns the whole cart, so each action just replaces what we show
// A customer's cart lives on the server, a visitor's lives in the browser and the API prices it
const EMPTY_CART = { items: [], totalQuantity: 0, subTotal: 0, shippingFee: 0, total: 0 };
const CartContext = React.createContext(null);

// Prices the visitor's lines and saves back what the API kept, so hidden or sold out products drop out
async function showGuestCart(items, setCart) {
	const priced = items.length > 0 ? await cartApi.quote(items) : EMPTY_CART;
	saveGuestCart(priced.items);
	setCart(priced);
	return priced;
}

export function CartProvider({ children }) {
	const { user } = useAuth();
	const [cart, setCart] = React.useState(EMPTY_CART);

	const refresh = React.useCallback(async () => {
		if (!user) return showGuestCart(readGuestCart(), setCart);

		const guestItems = readGuestCart();
		if (guestItems.length === 0) return setCart(await cartApi.get());

		// Cleared before the call so a second refresh cannot merge the same lines twice
		saveGuestCart([]);
		try {
			setCart(await cartApi.merge(guestItems));
		} catch (error) {
			saveGuestCart(guestItems);
			throw error;
		}
	}, [user]);

	React.useEffect(() => {
		refresh().catch(() => setCart(EMPTY_CART));
	}, [refresh]);

	const actions = React.useMemo(() => (user ? customerActions(setCart) : guestActions(setCart)), [user]);

	const value = React.useMemo(() => ({ cart, refresh, ...actions }), [cart, refresh, actions]);
	return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

const customerActions = (setCart) => ({
	add: async (productId, quantity) => setCart(await cartApi.addItem(productId, quantity)),
	setQuantity: async (productId, quantity) => setCart(await cartApi.setQuantity(productId, quantity)),
	remove: async (productId) => setCart(await cartApi.removeItem(productId)),
	clear: async () => setCart(await cartApi.clear()),
});

const guestActions = (setCart) => ({
	// The API caps a line at the stock, so a shorter line than asked means the stock ran out
	add: async (productId, quantity) => {
		const items = readGuestCart();
		const wanted = (items.find((item) => item.productId === productId)?.quantity ?? 0) + quantity;
		const priced = await showGuestCart(withQuantity(items, productId, wanted), setCart);

		const line = priced.items.find((item) => item.productId === productId);
		if (!line) throw new Error('This product is not available right now.');
		if (line.quantity < wanted) throw new Error(`Only ${line.quantity} of '${line.productName}' left, your cart has them all.`);
	},
	setQuantity: (productId, quantity) => showGuestCart(withQuantity(readGuestCart(), productId, quantity), setCart),
	remove: (productId) =>
		showGuestCart(
			readGuestCart().filter((item) => item.productId !== productId),
			setCart,
		),
	clear: () => showGuestCart([], setCart),
});

export function useCart() {
	return React.useContext(CartContext);
}
