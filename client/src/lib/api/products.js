import { api, toQueryString } from './client';
import { toProduct } from '../productHelpers';

export const productsApi = {
	// GET /products with category, q, minPrice, maxPrice, inStockOnly, newOnly, sort, page and pageSize
	listPage: async (query) => {
		const page = await api(`/products?${toQueryString(query)}`);
		return { ...page, items: page.items.map(toProduct) };
	},

	getBySlug: async (slug) => toProduct(await api(`/products/by-slug/${encodeURIComponent(slug)}`)),

	// A few more products for the product page, the same category first
	listRelated: async (id, count) => (await api(`/products/${id}/related?count=${count}`)).map(toProduct),
};
