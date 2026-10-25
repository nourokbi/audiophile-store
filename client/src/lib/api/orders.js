import { api } from './client';

// The signed in customer's own orders
export const ordersApi = {
	list: () => api('/orders'),
	get: (id) => api(`/orders/${id}`),

	// The same key on a retry returns the same order instead of placing a second one
	place: (addressId, paymentMethod, idempotencyKey) =>
		api('/orders', {
			method: 'POST',
			headers: { 'Idempotency-Key': idempotencyKey },
			body: { addressId, paymentMethod },
		}),

	cancel: (id) => api(`/orders/${id}/cancel`, { method: 'POST' }),
};
