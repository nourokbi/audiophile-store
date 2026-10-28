import { useMutation, useQueryClient } from '@tanstack/react-query';

import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

// Moves an order on, then marks the admin orders, the admin products and the shop as old so they load again
// Products are included because a cancel puts the items back in stock
// onSuccess returns the promise, so isPending stays true until the pages show the new data
export function useChangeOrderStatus() {
	const queryClient = useQueryClient();

	return useMutation({
		mutationFn: ({ orderId, status }) => adminApi.changeOrderStatus(orderId, status),
		onSuccess: () =>
			Promise.all([
				queryClient.invalidateQueries({ queryKey: queryKeys.adminOrders }),
				queryClient.invalidateQueries({ queryKey: queryKeys.adminProducts }),
				queryClient.invalidateQueries({ queryKey: queryKeys.catalog }),
			]),
	});
}
