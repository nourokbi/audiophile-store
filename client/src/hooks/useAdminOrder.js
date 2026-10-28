import { useQuery } from '@tanstack/react-query';

import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

// One order for the admin order page, a status change or a payment sync refreshes it through the cache
export function useAdminOrder(id) {
	const { data: order = null, error } = useQuery({ queryKey: queryKeys.adminOrder(id), queryFn: () => adminApi.getOrder(id) });

	const loadError = !error ? '' : error.status === 404 ? 'This order does not exist.' : error.message;
	return { order, loadError };
}
