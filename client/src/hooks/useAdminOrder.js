import React from 'react';

import { sampleOrdersApi as ordersSource } from '../data/adminOrdersSample';

// One order for the admin order page, with the status change that answers the updated order
export function useAdminOrder(id) {
	const [order, setOrder] = React.useState(null);
	const [loadError, setLoadError] = React.useState('');

	React.useEffect(() => {
		let isOutdated = false;
		ordersSource
			.getOrder(id)
			.then((loadedOrder) => !isOutdated && setOrder(loadedOrder))
			.catch((error) => !isOutdated && setLoadError(error.status === 404 ? 'This order does not exist.' : error.message));
		return () => {
			isOutdated = true;
		};
	}, [id]);

	const changeStatus = async (nextStatus) => setOrder(await ordersSource.changeOrderStatus(id, nextStatus));

	return { order, loadError, changeStatus };
}
