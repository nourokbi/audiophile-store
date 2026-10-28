import { keepPreviousData, useQuery } from '@tanstack/react-query';

import { useUrlFilters } from './useUrlFilters';
import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

const ORDER_FILTERS = { status: '', q: '', from: '', to: '', page: 1 };
const EMPTY_PAGE = { items: [], totalPages: 1, totalCount: 0, statusCounts: {} };

// The admin orders page: one page of orders, newest first, for the filters in the address bar, with the count per status
// The last page stays on screen while the next one loads, so the table does not flash empty
export function useAdminOrders() {
	const urlFilters = useUrlFilters(ORDER_FILTERS);
	const { filterQuery } = urlFilters;

	const {
		data: orderPage = EMPTY_PAGE,
		isPending,
		isPlaceholderData,
		error,
	} = useQuery({
		queryKey: queryKeys.adminOrderPage(filterQuery),
		queryFn: () => adminApi.listOrders(Object.fromEntries(new URLSearchParams(filterQuery))),
		placeholderData: keepPreviousData,
	});

	return {
		...urlFilters,
		orders: orderPage.items,
		totalCount: orderPage.totalCount,
		totalPages: orderPage.totalPages,
		statusCounts: orderPage.statusCounts,
		isLoading: isPending || isPlaceholderData,
		loadError: error?.message ?? '',
	};
}
