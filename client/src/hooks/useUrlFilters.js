import React from 'react';
import { useSearchParams } from 'react-router-dom';

const SEARCH_WAIT_MS = 300;

// Reads each filter from the address bar, the default's type says whether it is text, a number or true and false
function readFilters(searchParams, defaults) {
	return Object.fromEntries(
		Object.entries(defaults).map(([filterName, defaultValue]) => {
			const text = searchParams.get(filterName);
			if (text === null) return [filterName, defaultValue];
			if (typeof defaultValue === 'boolean') return [filterName, text === 'true'];
			if (typeof defaultValue === 'number') return [filterName, Number(text)];
			return [filterName, text];
		}),
	);
}

// The filters of an admin list kept in the address bar with the API's own names, so a refresh or a shared link keeps them
// A filter at its default leaves the address bar, every filter but page starts again on page 1, and q waits until typing stops
export function useUrlFilters(defaults) {
	const [searchParams, setSearchParams] = useSearchParams();
	const filters = readFilters(searchParams, defaults);
	const [searchText, setSearchText] = React.useState(filters.q ?? '');

	const setFilter = React.useCallback(
		(filterName, value) =>
			setSearchParams(
				(currentParams) => {
					const nextParams = new URLSearchParams(currentParams);
					if (value === defaults[filterName] || value === '') nextParams.delete(filterName);
					else nextParams.set(filterName, String(value));
					if (filterName !== 'page') nextParams.delete('page');
					return nextParams;
				},
				{ replace: true },
			),
		[setSearchParams, defaults],
	);

	const clearFilters = () => {
		setSearchText('');
		setSearchParams(new URLSearchParams(), { replace: true });
	};

	React.useEffect(() => {
		if (filters.q === undefined) return;
		const searchTimer = setTimeout(() => {
			if (searchText.trim() !== filters.q) setFilter('q', searchText.trim());
		}, SEARCH_WAIT_MS);
		return () => clearTimeout(searchTimer);
	}, [searchText, filters.q, setFilter]);

	// Only the known filters, ready to hand to an adminApi list call
	const filterQuery = new URLSearchParams(
		[...searchParams].filter(([filterName]) => Object.hasOwn(defaults, filterName)),
	).toString();

	return {
		filters,
		filterQuery,
		hasFilters: [...searchParams.keys()].some((filterName) => filterName !== 'page' && Object.hasOwn(defaults, filterName)),
		searchText,
		setSearchText,
		setFilter,
		clearFilters,
	};
}
