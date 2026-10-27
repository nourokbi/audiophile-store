import { api } from './client';

// The signed in customer's saved shipping addresses
export const addressesApi = {
	list: () => api('/addresses'),
	create: (address) => api('/addresses', { method: 'POST', body: address }),
	remove: (id) => api(`/addresses/${id}`, { method: 'DELETE' }),
};
