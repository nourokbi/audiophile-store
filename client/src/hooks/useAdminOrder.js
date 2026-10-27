import React from 'react';

import { adminApi } from '../lib/api/admin';

// One order for the admin order page, with the status change that answers the updated order and a reload
export function useAdminOrder(id) {
	const [order, setOrder] = React.useState(null);
	const [loadError, setLoadError] = React.useState('');

	React.useEffect(() => {
		let isOutdated = false;
		adminApi
			.getOrder(id)
			.then((loadedOrder) => !isOutdated && setOrder(loadedOrder))
			.catch((error) => !isOutdated && setLoadError(error.status === 404 ? 'This order does not exist.' : error.message));
		return () => {
			isOutdated = true;
		};
	}, [id]);

	const changeStatus = async (nextStatus) => setOrder(await adminApi.changeOrderStatus(id, nextStatus));
	const reload = async () => setOrder(await adminApi.getOrder(id));

	return { order, loadError, changeStatus, reload };
}
