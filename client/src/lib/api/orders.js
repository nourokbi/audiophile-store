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

	// A new Fawaterak payment page for an unpaid card order, the old one closes after a failed attempt
	retryPayment: (id) => api(`/orders/${id}/payment/retry`, { method: 'POST' }),
};
