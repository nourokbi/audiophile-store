import { api } from './client';
import { toProduct } from '../productHelpers';

// Every call the admin dashboard makes, each answer is turned into the shop's product shape so a row can be swapped in
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
};
