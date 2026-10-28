import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

// What Fawaterak sent about a card order, newest first, and the sync that asks Fawaterak again when a webhook seems lost
export function usePaymentPanel(orderId, isCardOrder) {
	const queryClient = useQueryClient();

	const { data: events = [], error } = useQuery({
		queryKey: queryKeys.paymentEvents(orderId),
		queryFn: async () => (await adminApi.listPaymentEvents(orderId)).reverse(),
		enabled: isCardOrder, // a cash order never talks to Fawaterak
	});

	// A sync stores a new event and can move the order, so the whole admin orders group loads again, these events included
	const sync = useMutation({
		mutationFn: () => adminApi.syncPayment(orderId),
		onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.adminOrders }),
	});

	return {
		events,
		loadError: error?.message ?? '',
		isSyncing: sync.isPending,
		syncError: sync.error?.message ?? '',
		syncWithGateway: () => sync.mutate(),
	};
}
