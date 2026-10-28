import { useMutation, useQueryClient } from '@tanstack/react-query';

import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

// Moves an order on, then every admin order list, open order and number loads again
// Cancelling puts the items back in stock, so the product lists and the shop load again too
export function useChangeOrderStatus() {
	const queryClient = useQueryClient();

	return useMutation({
		mutationFn: ({ orderId, status }) => adminApi.changeOrderStatus(orderId, status),
		onSuccess: (_order, { status }) => {
			if (status === 'Cancelled') {
				queryClient.invalidateQueries({ queryKey: queryKeys.adminProducts });
				queryClient.invalidateQueries({ queryKey: queryKeys.catalog });
			}
			return queryClient.invalidateQueries({ queryKey: queryKeys.adminOrders });
		},
	});
}
