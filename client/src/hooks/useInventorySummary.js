import { useQuery } from '@tanstack/react-query';

import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

const EMPTY_SUMMARY = { onSaleCount: null, lowStockCount: null, lowStockProducts: [] };

// The whole catalogue's numbers for the overview, read from two small filtered pages until GET /admin/stats gives them in one call
export function useInventorySummary(previewCount) {
	const { data: summary = EMPTY_SUMMARY, error } = useQuery({
		queryKey: queryKeys.inventorySummary(previewCount),
		queryFn: async () => {
			const [onSalePage, lowStockPage] = await Promise.all([
				adminApi.listProducts({ availability: 'OnSale', pageSize: 1 }),
				adminApi.listProducts({ availability: 'OnSale', lowStockOnly: true, sort: 'StockLowToHigh', pageSize: previewCount }),
			]);
			return {
				onSaleCount: onSalePage.totalCount,
				lowStockCount: lowStockPage.totalCount,
				lowStockProducts: lowStockPage.items,
			};
		},
	});

	return { ...summary, loadError: error?.message ?? '' };
}
