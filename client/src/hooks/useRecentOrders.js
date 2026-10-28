import { useQuery } from '@tanstack/react-query';

import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

// The newest orders for the overview preview
export function useRecentOrders(count) {
	const { data: recentOrders = [] } = useQuery({
		queryKey: queryKeys.recentOrders(count),
		queryFn: async () => (await adminApi.listOrders({ pageSize: count })).items,
	});

	return recentOrders;
}
