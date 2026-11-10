import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

// What Fawaterak sent about a card order, newest first, the sync that asks Fawaterak again when a webhook seems lost,
// and recording a refund
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

	// Records a refund sent from the Fawaterak dashboard, the customer gets an email and the order shows it refunded
	const refund = useMutation({
		mutationFn: (reference) => adminApi.refundOrder(orderId, reference),
		onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.adminOrders }),
	});

	return {
		events,
		isRefunding: refund.isPending,
		refundError: refund.error?.message ?? '',
		markRefunded: (reference) => refund.mutate(reference),
		loadError: error?.message ?? '',
		isSyncing: sync.isPending,
		syncError: sync.error?.message ?? '',
		syncWithGateway: () => sync.mutate(),
	};
}
