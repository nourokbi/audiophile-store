// A visitor's cart as [{ productId, quantity }] in localStorage, the API prices it and it moves into the account at login
const GUEST_CART_KEY = 'audiophile.guestCart';

export function readGuestCart() {
	try {
		const items = JSON.parse(localStorage.getItem(GUEST_CART_KEY));
		return Array.isArray(items) ? items : [];
	} catch {
		return [];
	}
}

export function saveGuestCart(items) {
	try {
		if (items.length === 0) localStorage.removeItem(GUEST_CART_KEY);
		else localStorage.setItem(GUEST_CART_KEY, JSON.stringify(items.map(({ productId, quantity }) => ({ productId, quantity }))));
	} catch {
		// With storage blocked the cart still lives until the tab closes
	}
}

// Sets one product's quantity, adding the line when it is new
export function withQuantity(items, productId, quantity) {
	return [...items.filter((item) => item.productId !== productId), { productId, quantity }];
}
