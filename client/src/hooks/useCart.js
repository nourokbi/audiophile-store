import { useQuery, useQueryClient } from '@tanstack/react-query';

import { useAuth } from '../context/AuthContext';
import { cartApi } from '../lib/api/cart';
import { readGuestCart, saveGuestCart, withQuantity } from '../lib/guestCart';
import { queryKeys } from '../lib/queryClient';

// Every cart endpoint returns the whole cart, so each action just puts the answer in the cache
// A customer's cart lives on the server, a visitor's lives in the browser and the API prices it
const EMPTY_CART = { items: [], totalQuantity: 0, subTotal: 0, shippingFee: 0, total: 0 };

// Prices the visitor's lines and saves back what the API kept, so hidden or sold out products drop out
async function priceGuestCart(items) {
	const priced = items.length > 0 ? await cartApi.quote(items) : EMPTY_CART;
	saveGuestCart(priced.items);
	return priced;
}

// Right after login the visitor's cart is merged in first, cleared before the call so a second load cannot merge it twice
async function loadCustomerCart() {
	const guestItems = readGuestCart();
	if (guestItems.length === 0) return cartApi.get();

	saveGuestCart([]);
	try {
		return await cartApi.merge(guestItems);
	} catch (error) {
		saveGuestCart(guestItems);
		throw error;
	}
}

// Every component that calls it shares one cart in the cache, it waits for the session restore so it loads the right one
export function useCart() {
	const { user, isRestoring } = useAuth();
	const queryClient = useQueryClient();
	const cartKey = queryKeys.cart(user?.id);

	const { data: cart = EMPTY_CART } = useQuery({
		queryKey: cartKey,
		queryFn: user ? loadCustomerCart : () => priceGuestCart(readGuestCart()),
		enabled: !isRestoring,
	});

	const showCart = (nextCart) => queryClient.setQueryData(cartKey, nextCart);
	const actions = user ? customerActions(showCart) : guestActions(showCart);

	return { cart, refresh: () => queryClient.invalidateQueries({ queryKey: cartKey }), ...actions };
}

const customerActions = (showCart) => ({
	add: async (productId, quantity) => showCart(await cartApi.addItem(productId, quantity)),
	setQuantity: async (productId, quantity) => showCart(await cartApi.setQuantity(productId, quantity)),
	remove: async (productId) => showCart(await cartApi.removeItem(productId)),
	clear: async () => showCart(await cartApi.clear()),
});

const guestActions = (showCart) => ({
	// The API caps a line at the stock, so a shorter line than asked means the stock ran out
	add: async (productId, quantity) => {
		const items = readGuestCart();
		const wanted = (items.find((item) => item.productId === productId)?.quantity ?? 0) + quantity;
		const priced = showCart(await priceGuestCart(withQuantity(items, productId, wanted)));

		const line = priced.items.find((item) => item.productId === productId);
		if (!line) throw new Error('This product is not available right now.');
		if (line.quantity < wanted) throw new Error(`Only ${line.quantity} of '${line.productName}' left, your cart has them all.`);
	},
	setQuantity: async (productId, quantity) => showCart(await priceGuestCart(withQuantity(readGuestCart(), productId, quantity))),
	remove: async (productId) => showCart(await priceGuestCart(readGuestCart().filter((item) => item.productId !== productId))),
	clear: async () => showCart(await priceGuestCart([])),
});
