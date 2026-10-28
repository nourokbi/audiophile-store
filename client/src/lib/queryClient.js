import { QueryClient } from '@tanstack/react-query';

import { ApiError } from './api/client';

// One cache for every API answer, a page that asks for data already in it shows it at once
// An answer counts as fresh for 30 seconds, after that a page showing it fetches again in the background
// A failed request is tried once more, but never a 4xx, since asking again gives the same answer
export const queryClient = new QueryClient({
	defaultOptions: {
		queries: {
			staleTime: 30 * 1000,
			retry: (failureCount, error) => failureCount < 1 && !(error instanceof ApiError && error.status < 500),
		},
	},
});

// Each key goes from the broad group to the details, so invalidating a group reaches everything under it
// Invalidating adminOrders, for example, refreshes the order list, each open order, the stats and the payment events
export const queryKeys = {
	catalog: ['catalog'],
	cart: (userId) => ['cart', userId ?? 'guest'],
	addresses: ['addresses'],
	myOrders: ['my-orders'],
	myOrder: (id) => ['my-orders', id],
	adminProducts: ['admin', 'products'],
	adminProductPage: (filterQuery) => ['admin', 'products', 'page', filterQuery],
	inventorySummary: (previewCount) => ['admin', 'products', 'summary', previewCount],
	adminOrders: ['admin', 'orders'],
	adminOrderPage: (filterQuery) => ['admin', 'orders', 'page', filterQuery],
	recentOrders: (count) => ['admin', 'orders', 'recent', count],
	adminOrder: (id) => ['admin', 'orders', 'one', id],
	orderStats: (days) => ['admin', 'orders', 'stats', days],
	paymentEvents: (orderId) => ['admin', 'orders', 'payment-events', orderId],
};
