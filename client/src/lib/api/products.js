import { api } from './client';
import { toProduct } from '../productHelpers';

const LARGEST_PAGE = 50; // ProductQuery.MaxPageSize in the API

export const productsApi = {
	// Every product on sale, the first page tells how many pages there are and the others load together
	listAll: async () => {
		const pageUrl = (page) => `/products?pageSize=${LARGEST_PAGE}&page=${page}`;
		const firstPage = await api(pageUrl(1));
		const otherPages = await Promise.all(Array.from({ length: firstPage.totalPages - 1 }, (_, index) => api(pageUrl(index + 2))));
		return [firstPage, ...otherPages].flatMap((page) => page.items).map(toProduct);
	},
};
