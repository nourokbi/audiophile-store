import React from 'react';

import { useUrlFilters } from './useUrlFilters';
import { adminApi } from '../lib/api/admin';

const ORDER_FILTERS = { status: '', q: '', from: '', to: '', page: 1 };
const EMPTY_PAGE = { items: [], totalPages: 1, totalCount: 0, statusCounts: {} };

// The admin orders page: one page of orders, newest first, for the filters in the address bar, with the count per status
export function useAdminOrders() {
	const urlFilters = useUrlFilters(ORDER_FILTERS);
	const [orderPage, setOrderPage] = React.useState(EMPTY_PAGE);
	const [isLoading, setIsLoading] = React.useState(true);
	const [loadError, setLoadError] = React.useState('');

	const { filterQuery } = urlFilters;
	React.useEffect(() => {
		let isOutdated = false;
		setIsLoading(true);
		adminApi
			.listOrders(Object.fromEntries(new URLSearchParams(filterQuery)))
			.then((loadedPage) => {
				if (isOutdated) return;
				setOrderPage(loadedPage);
				setLoadError('');
			})
			.catch((error) => !isOutdated && setLoadError(error.message))
			.finally(() => !isOutdated && setIsLoading(false));
		return () => {
			isOutdated = true;
		};
	}, [filterQuery]);

	// The row takes the order the server answered, and the tab counts move with it
	const replaceOrder = (updatedOrder) =>
		setOrderPage((currentPage) => {
			const previousOrder = currentPage.items.find((order) => order.id === updatedOrder.id);
			const statusCounts = { ...currentPage.statusCounts };
			if (previousOrder && previousOrder.status !== updatedOrder.status) {
				statusCounts[previousOrder.status] -= 1;
				statusCounts[updatedOrder.status] = (statusCounts[updatedOrder.status] ?? 0) + 1;
			}
			return {
				...currentPage,
				statusCounts,
				items: currentPage.items.map((order) => (order.id === updatedOrder.id ? { ...order, ...updatedOrder } : order)),
			};
		});

	return {
		...urlFilters,
		orders: orderPage.items,
		totalCount: orderPage.totalCount,
		totalPages: orderPage.totalPages,
		statusCounts: orderPage.statusCounts,
		isLoading,
		loadError,
		changeOrderStatus: adminApi.changeOrderStatus,
		replaceOrder,
	};
}
