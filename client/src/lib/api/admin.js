import { api } from './client';
import { toProduct } from '../productHelpers';

// Every call the admin dashboard makes, each product answer is turned into the shop's product shape so a row can be swapped in
const toPage = (page) => ({ ...page, items: page.items.map(toProduct) });

// Leaves out the empty filters so the API uses its defaults for them
const toQueryString = (filters) =>
	new URLSearchParams(
		Object.entries(filters).filter(([, value]) => value !== undefined && value !== null && value !== ''),
	).toString();

export const adminApi = {
	// GET /admin/products with q, category, availability, lowStockOnly, sort, page and pageSize, hidden products included,
	// answers { items, page, pageSize, totalCount, totalPages }
	listProducts: async (filters = {}) => toPage(await api(`/admin/products?${toQueryString(filters)}`)),

	// POST /admin/products with the ProductRequest fields
	createProduct: async (product) => toProduct(await api('/admin/products', { method: 'POST', body: product })),

	// PUT /admin/products/{id} with UpdateProductRequest, the details and price, never the stock
	updateProduct: async (id, product) => toProduct(await api(`/admin/products/${id}`, { method: 'PUT', body: product })),

	// POST /admin/products/{id}/restock with { quantity }, adds units to the stock
	restock: async (id, quantity) => toProduct(await api(`/admin/products/${id}/restock`, { method: 'POST', body: { quantity } })),

	// PUT /admin/products/{id}/price with { price }
	changePrice: async (id, price) => toProduct(await api(`/admin/products/${id}/price`, { method: 'PUT', body: { price } })),

	// PUT /admin/products/{id}/availability with { isActive }, a hidden product is not sold in the shop
	setAvailability: async (id, isActive) =>
		toProduct(await api(`/admin/products/${id}/availability`, { method: 'PUT', body: { isActive } })),

	// GET /admin/orders with status, q, from, to, page and pageSize, newest first,
	// answers { items, page, pageSize, totalCount, totalPages, statusCounts }, the counts ignore the status filter
	listOrders: (filters = {}) => api(`/admin/orders?${toQueryString(filters)}`),

	// GET /admin/orders/{id}, the summary plus shipping and items
	getOrder: (id) => api(`/admin/orders/${id}`),

	// PUT /admin/orders/{id}/status with { status }, only a status in the order's nextStatuses is accepted, any other is a 409
	changeOrderStatus: (id, status) => api(`/admin/orders/${id}/status`, { method: 'PUT', body: { status } }),

	// GET /orders/{id}/payment/events, what Fawaterak sent about the order, oldest first
	listPaymentEvents: (orderId) => api(`/orders/${orderId}/payment/events`),

	// POST /orders/{id}/payment/sync, asks Fawaterak for the invoice and applies it, a 409 when the order has no invoice
	syncPayment: (orderId) => api(`/orders/${orderId}/payment/sync`, { method: 'POST' }),
};
