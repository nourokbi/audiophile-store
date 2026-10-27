import React from 'react';

import { adminApi } from '../lib/api/admin';

// What Fawaterak sent about a card order, newest first, and the sync that asks Fawaterak again when a webhook seems lost
export function usePaymentPanel(orderId, isCardOrder, onSynced) {
	const [events, setEvents] = React.useState([]);
	const [loadError, setLoadError] = React.useState('');
	const [reloadCount, setReloadCount] = React.useState(0); // raised after a sync, which stores a new event
	const [isSyncing, setIsSyncing] = React.useState(false);
	const [syncError, setSyncError] = React.useState('');

	React.useEffect(() => {
		if (!isCardOrder) return; // a cash order never talks to Fawaterak
		let isOutdated = false;
		adminApi
			.listPaymentEvents(orderId)
			.then((loadedEvents) => {
				if (isOutdated) return;
				setEvents([...loadedEvents].reverse());
				setLoadError('');
			})
			.catch((error) => !isOutdated && setLoadError(error.message));
		return () => {
			isOutdated = true;
		};
	}, [orderId, isCardOrder, reloadCount]);

	const syncWithGateway = async () => {
		setIsSyncing(true);
		setSyncError('');
		try {
			await adminApi.syncPayment(orderId);
			await onSynced();
			setReloadCount((count) => count + 1);
		} catch (error) {
			setSyncError(error.message);
		} finally {
			setIsSyncing(false);
		}
	};

	return { events, loadError, isSyncing, syncError, syncWithGateway };
}
