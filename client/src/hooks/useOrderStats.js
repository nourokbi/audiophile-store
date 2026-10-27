import React from 'react';

import { adminApi } from '../lib/api/admin';

// The order numbers for the overview and income pages, stats stays null until the first answer
export function useOrderStats(days) {
	const [stats, setStats] = React.useState(null);
	const [loadError, setLoadError] = React.useState('');

	React.useEffect(() => {
		let isOutdated = false;
		adminApi
			.getOrderStats(days)
			.then((loadedStats) => !isOutdated && setStats(loadedStats))
			.catch((error) => !isOutdated && setLoadError(error.message));
		return () => {
			isOutdated = true;
		};
	}, [days]);

	return { stats, loadError };
}
