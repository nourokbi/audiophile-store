import React from 'react';

export const PRICE_RANGES = [
	{ key: 'all', label: 'Any price', min: 0, max: Infinity },
	{ key: 'under-1000', label: 'Under EGP 1,000', min: 0, max: 999 },
	{ key: '1000-3000', label: 'EGP 1,000 to 3,000', min: 1000, max: 3000 },
	{ key: '3000-5000', label: 'EGP 3,000 to 5,000', min: 3000, max: 5000 },
	{ key: 'over-5000', label: 'Over EGP 5,000', min: 5000, max: Infinity },
];

export const SORT_ORDERS = {
	featured: { label: 'Featured', compare: (a, b) => Number(b.isNew) - Number(a.isNew) || b.price - a.price },
	'price-asc': { label: 'Price, low to high', compare: (a, b) => a.price - b.price },
	'price-desc': { label: 'Price, high to low', compare: (a, b) => b.price - a.price },
	name: { label: 'Name, A to Z', compare: (a, b) => a.name.localeCompare(b.name) },
};

const EMPTY_FILTERS = { searchText: '', priceRange: 'all', sortOrder: 'featured', inStockOnly: false };
const PAGE_SIZE = 9;

// Filters, sorts and pages the catalogue in the browser for the storefront, a new category or collection starts on page 1
export function useShopFilters(products, categoryKey, collection) {
	const [filters, setFilters] = React.useState(EMPTY_FILTERS);
	const [requestedPage, setRequestedPage] = React.useState(1);

	React.useEffect(() => setRequestedPage(1), [categoryKey, collection]);

	const setFilter = (filterName, value) => {
		setFilters((currentFilters) => ({ ...currentFilters, [filterName]: value }));
		setRequestedPage(1);
	};
	const clearFilters = () => {
		setFilters(EMPTY_FILTERS);
		setRequestedPage(1);
	};

	const priceRange = PRICE_RANGES.find((range) => range.key === filters.priceRange);
	const searchTerm = filters.searchText.trim().toLowerCase();
	const matchingProducts = products
		.filter(
			(product) =>
				(collection !== 'new' || product.isNew) &&
				(categoryKey === 'all' || product.category === categoryKey) &&
				(!filters.inStockOnly || product.inStock) &&
				product.price >= priceRange.min &&
				product.price <= priceRange.max &&
				(!searchTerm ||
					product.name.toLowerCase().includes(searchTerm) ||
					product.description.toLowerCase().includes(searchTerm)),
		)
		.sort(SORT_ORDERS[filters.sortOrder].compare);

	const totalPages = Math.max(1, Math.ceil(matchingProducts.length / PAGE_SIZE));
	const currentPage = Math.min(requestedPage, totalPages);
	const firstIndex = (currentPage - 1) * PAGE_SIZE;

	return {
		filters,
		hasFilters: JSON.stringify(filters) !== JSON.stringify(EMPTY_FILTERS),
		setFilter,
		clearFilters,
		matchingCount: matchingProducts.length,
		pageProducts: matchingProducts.slice(firstIndex, firstIndex + PAGE_SIZE),
		firstIndex,
		currentPage,
		totalPages,
		setCurrentPage: setRequestedPage,
	};
}
