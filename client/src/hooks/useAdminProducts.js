import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';

import { useUrlFilters } from './useUrlFilters';
import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

const PRODUCT_FILTERS = { q: '', category: '', availability: 'All', lowStockOnly: false, sort: 'Name', page: 1 };
const EMPTY_PAGE = { items: [], totalPages: 1, totalCount: 0 };

// The admin products page: one page of products, hidden ones included, for the filters in the address bar
export function useAdminProducts() {
	const queryClient = useQueryClient();
	const urlFilters = useUrlFilters(PRODUCT_FILTERS);
	const { filterQuery } = urlFilters;

	const { data: productPage = EMPTY_PAGE, error } = useQuery({
		queryKey: queryKeys.adminProductPage(filterQuery),
		queryFn: () => adminApi.listProducts(Object.fromEntries(new URLSearchParams(filterQuery))),
		placeholderData: keepPreviousData,
	});

	// After any change the admin lists and the shop load again, so a shown or hidden product appears in the shop at once
	const refreshProducts = () =>
		Promise.all([
			queryClient.invalidateQueries({ queryKey: queryKeys.adminProducts }),
			queryClient.invalidateQueries({ queryKey: queryKeys.catalog }),
		]);

	// A new product can land on any page, so the table goes back to every product on page 1
	const reloadFromFirstPage = () => {
		urlFilters.clearFilters();
		return refreshProducts();
	};

	return {
		...urlFilters,
		products: productPage.items,
		totalCount: productPage.totalCount,
		totalPages: productPage.totalPages,
		loadError: error?.message ?? '',
		refreshProducts,
		reloadFromFirstPage,
	};
}
