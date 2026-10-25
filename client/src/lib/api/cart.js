import { api } from './client';

// Every cart endpoint answers with the whole cart
export const cartApi = {
	get: () => api('/cart'),
	addItem: (productId, quantity) => api('/cart/items', { method: 'POST', body: { productId, quantity } }),
	setQuantity: (productId, quantity) => api(`/cart/items/${productId}`, { method: 'PUT', body: { quantity } }),
	removeItem: (productId) => api(`/cart/items/${productId}`, { method: 'DELETE' }),
	clear: () => api('/cart', { method: 'DELETE' }),
};
