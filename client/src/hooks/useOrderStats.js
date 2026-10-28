import { keepPreviousData, useQuery } from '@tanstack/react-query';

import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

// The order numbers for the overview and income pages, stats stays null until the first answer
// Both pages ask for 30 days by default so they share one answer, and a new period keeps the old charts until it loads
export function useOrderStats(days) {
	const { data: stats = null, error } = useQuery({
		queryKey: queryKeys.orderStats(days),
		queryFn: () => adminApi.getOrderStats(days),
		placeholderData: keepPreviousData,
	});

	return { stats, loadError: error?.message ?? '' };
}
