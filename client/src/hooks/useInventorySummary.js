import React from 'react';

import { adminApi } from '../lib/api/admin';

// The whole catalogue's numbers for the overview, read from two small filtered pages until GET /admin/stats gives them in one call
export function useInventorySummary(previewCount) {
	const [summary, setSummary] = React.useState({ onSaleCount: null, lowStockCount: null, lowStockProducts: [] });
	const [loadError, setLoadError] = React.useState('');

	React.useEffect(() => {
		Promise.all([
			adminApi.listProducts({ availability: 'OnSale', pageSize: 1 }),
			adminApi.listProducts({ availability: 'OnSale', lowStockOnly: true, sort: 'StockLowToHigh', pageSize: previewCount }),
		])
			.then(([onSalePage, lowStockPage]) =>
				setSummary({
					onSaleCount: onSalePage.totalCount,
					lowStockCount: lowStockPage.totalCount,
					lowStockProducts: lowStockPage.items,
				}),
			)
			.catch((error) => setLoadError(error.message));
	}, [previewCount]);

	return { ...summary, loadError };
}
