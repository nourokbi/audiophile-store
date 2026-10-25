import { api } from './api';
import { toProduct } from '../context/CatalogContext';

// Every call the admin dashboard makes, each answer is turned into the shop's product shape so a row can be swapped in
const toPage = (page) => ({ ...page, items: page.items.map(toProduct) });

export const adminApi = {
	// GET /admin/products, hidden products included, answers { items, page, pageSize, totalCount, totalPages }
	listProducts: async (page = 1) => toPage(await api(`/admin/products?page=${page}`)),

	// GET /admin/products/search?q=, the same page shape as the list
	searchProducts: async (q, page = 1) =>
		toPage(await api(`/admin/products/search?q=${encodeURIComponent(q)}&page=${page}`)),

	// POST /admin/products with the ProductRequest fields
	createProduct: async (product) => toProduct(await api('/admin/products', { method: 'POST', body: product })),

	// PUT /admin/products/{id} replaces every field
	updateProduct: async (id, product) => toProduct(await api(`/admin/products/${id}`, { method: 'PUT', body: product })),

	// POST /admin/products/{id}/restock with { quantity }, adds units to the stock
	restock: async (id, quantity) =>
		toProduct(await api(`/admin/products/${id}/restock`, { method: 'POST', body: { quantity } })),

	// PUT /admin/products/{id}/price with { price }
	changePrice: async (id, price) => toProduct(await api(`/admin/products/${id}/price`, { method: 'PUT', body: { price } })),

	// PUT /admin/products/{id}/availability with { isActive }, a hidden product is not sold in the shop
	setAvailability: async (id, isActive) =>
		toProduct(await api(`/admin/products/${id}/availability`, { method: 'PUT', body: { isActive } })),
};
