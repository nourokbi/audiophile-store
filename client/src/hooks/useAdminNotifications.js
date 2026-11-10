import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { adminApi } from '../lib/api/admin';
import { queryKeys } from '../lib/queryClient';

// Asked again every 30 seconds while the dashboard is open, so a new order shows up without a refresh
const REFRESH_MS = 30_000;

// The bell's notifications, the newest first, with the unread count for the menu badge
export function useAdminNotifications() {
	const queryClient = useQueryClient();
	const { data, isPending, isError } = useQuery({
		queryKey: queryKeys.adminNotifications,
		queryFn: () => adminApi.listNotifications(),
		refetchInterval: REFRESH_MS,
	});

	const refresh = () => queryClient.invalidateQueries({ queryKey: queryKeys.adminNotifications });
	const markRead = useMutation({ mutationFn: adminApi.markNotificationRead, onSuccess: refresh });
	const markAllRead = useMutation({ mutationFn: adminApi.markAllNotificationsRead, onSuccess: refresh });

	return {
		notifications: data?.items ?? [],
		unreadCount: data?.unreadCount ?? 0,
		isPending,
		isError,
		markRead: (id) => markRead.mutate(id),
		markAllRead: () => markAllRead.mutate(),
		isMarkingAll: markAllRead.isPending,
	};
}
