import React from 'react';

import { adminApi } from '../lib/api/admin';

// The newest orders for the overview preview
export function useRecentOrders(count) {
	const [recentOrders, setRecentOrders] = React.useState([]);

	React.useEffect(() => {
		let isOutdated = false;
		adminApi
			.listOrders({ pageSize: count })
			.then((orderPage) => !isOutdated && setRecentOrders(orderPage.items))
			.catch(() => !isOutdated && setRecentOrders([]));
		return () => {
			isOutdated = true;
		};
	}, [count]);

	return recentOrders;
}
