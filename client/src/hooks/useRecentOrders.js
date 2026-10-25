import React from 'react';

import { sampleOrdersApi as ordersSource } from '../data/adminOrdersSample';

// The newest orders for the overview preview
export function useRecentOrders(count) {
	const [recentOrders, setRecentOrders] = React.useState([]);

	React.useEffect(() => {
		let isOutdated = false;
		ordersSource
			.listOrders({ pageSize: count })
			.then((orderPage) => !isOutdated && setRecentOrders(orderPage.items))
			.catch(() => !isOutdated && setRecentOrders([]));
		return () => {
			isOutdated = true;
		};
	}, [count]);

	return recentOrders;
}
