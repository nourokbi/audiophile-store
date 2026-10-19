// Every call the admin dashboard makes, in one place so connecting the admin API means editing only this file
// Until an endpoint exists its function answers with what it was given, and the dashboard keeps the change on the page
// To connect one, replace its body with the api call in its comment, for example
// restock: (id, quantity) => api(`/admin/products/${id}/restock`, { method: 'POST', body: { quantity } }),

export const adminApi = {
	// POST /admin/products/{id}/restock with { quantity }, adds units to the stock
	restock: async (id, quantity) => ({ id, quantity }),

	// PUT /admin/products/{id}/price with { price }
	changePrice: async (id, price) => ({ id, price }),

	// PUT /admin/products/{id}/availability with { isActive }, a hidden product is not sold in the shop
	setAvailability: async (id, isActive) => ({ id, isActive }),
};

// Set to true once the functions above call the real API, the dashboard then stops showing the not saved banner
export const ADMIN_API_CONNECTED = false;
