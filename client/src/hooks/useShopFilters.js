import React from 'react';

import { useCatalogPage } from './useCatalog';
import { findCategory } from '../lib/navigation';

export const PRICE_RANGES = [
	{ key: 'all', label: 'Any price' },
	{ key: 'under-1000', label: 'Under EGP 1,000', max: 999.99 },
	{ key: '1000-3000', label: 'EGP 1,000 to 3,000', min: 1000, max: 3000 },
	{ key: '3000-5000', label: 'EGP 3,000 to 5,000', min: 3000, max: 5000 },
	{ key: 'over-5000', label: 'Over EGP 5,000', min: 5000 },
];

export const SORT_ORDERS = {
	featured: { label: 'Featured', apiSort: 'Featured' },
	'price-asc': { label: 'Price, low to high', apiSort: 'PriceLowToHigh' },
	'price-desc': { label: 'Price, high to low', apiSort: 'PriceHighToLow' },
	name: { label: 'Name, A to Z', apiSort: 'Name' },
};

// Multiples of three, so the last row of the grid is always full
export const PAGE_SIZES = [9, 18, 36, 72];

const EMPTY_FILTERS = { searchText: '', priceRange: 'all', sortOrder: 'featured', inStockOnly: false, pageSize: PAGE_SIZES[0] };
const SEARCH_WAIT_MS = 300;

const toApiCategory = (categoryKey) => categoryKey[0].toUpperCase() + categoryKey.slice(1);

// The storefront's filters and the one page of products the API returns for them, a new category or collection starts on page 1
export function useShopFilters(categoryKey, collection) {
	const [filters, setFilters] = React.useState(EMPTY_FILTERS);
	const [requestedPage, setRequestedPage] = React.useState(1);
	const [searchTerm, setSearchTerm] = React.useState('');

	React.useEffect(() => setRequestedPage(1), [categoryKey, collection]);

	// Searches once typing stops, not on every key
	React.useEffect(() => {
		const timer = setTimeout(() => setSearchTerm(filters.searchText.trim()), SEARCH_WAIT_MS);
		return () => clearTimeout(timer);
	}, [filters.searchText]);

	const setFilter = (filterName, value) => {
		setFilters((currentFilters) => ({ ...currentFilters, [filterName]: value }));
		setRequestedPage(1);
	};
	const clearFilters = () => {
		setFilters(EMPTY_FILTERS);
		setRequestedPage(1);
	};

	const priceRange = PRICE_RANGES.find((range) => range.key === filters.priceRange);
	const query = {
		category: categoryKey === 'all' ? undefined : toApiCategory(categoryKey),
		q: searchTerm,
		minPrice: priceRange.min,
		maxPrice: priceRange.max,
		inStockOnly: filters.inStockOnly || undefined,
		newOnly: collection === 'new' || undefined,
		sort: SORT_ORDERS[filters.sortOrder].apiSort,
		page: requestedPage,
		pageSize: filters.pageSize,
	};

	const { data, isPending, isPlaceholderData, error } = useCatalogPage(query, categoryKey === 'all' || Boolean(findCategory(categoryKey)));
	const totalPages = Math.max(1, data?.totalPages ?? 1);

	// Products can disappear while a shopper is on the last page, so an empty page past the end moves back to the new last page
	React.useEffect(() => {
		if (data && !isPlaceholderData && requestedPage > totalPages) setRequestedPage(totalPages);
	}, [data, isPlaceholderData, requestedPage, totalPages]);

	return {
		filters,
		hasFilters: JSON.stringify(filters) !== JSON.stringify(EMPTY_FILTERS),
		setFilter,
		clearFilters,
		pageProducts: data?.items ?? [],
		firstIndex: ((data?.page ?? 1) - 1) * filters.pageSize,
		currentPage: requestedPage,
		totalPages,
		setCurrentPage: setRequestedPage,
		loading: isPending,
		updating: isPlaceholderData,
		error: error?.message ?? null,
	};
}
