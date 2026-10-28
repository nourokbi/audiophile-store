import { api } from './client';

// Every cart endpoint answers with the whole cart, quote prices a visitor's cart and merge moves it into the account
export const cartApi = {
	get: () => api('/cart'),
	addItem: (productId, quantity) => api('/cart/items', { method: 'POST', body: { productId, quantity } }),
	setQuantity: (productId, quantity) => api(`/cart/items/${productId}`, { method: 'PUT', body: { quantity } }),
	removeItem: (productId) => api(`/cart/items/${productId}`, { method: 'DELETE' }),
	clear: () => api('/cart', { method: 'DELETE' }),
	quote: (items) => api('/cart/quote', { method: 'POST', body: items }),
	merge: (items) => api('/cart/merge', { method: 'POST', body: items }),
};
