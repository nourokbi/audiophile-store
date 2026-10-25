import React from 'react';

import { useUrlFilters } from './useUrlFilters';
import { useCatalog } from '../context/CatalogContext';
import { adminApi } from '../lib/api/admin';

const PRODUCT_FILTERS = { q: '', category: '', availability: 'All', lowStockOnly: false, sort: 'Name', page: 1 };

// The admin products page: one page of products, hidden ones included, for the filters in the address bar
export function useAdminProducts() {
	const { reload: reloadShopCatalog } = useCatalog();
	const urlFilters = useUrlFilters(PRODUCT_FILTERS);
	const [productPage, setProductPage] = React.useState({ items: [], totalPages: 1, totalCount: 0 });
	const [reloadCount, setReloadCount] = React.useState(0); // raised to load the same page again
	const [loadError, setLoadError] = React.useState('');

	// An answer that arrives after a newer request is ignored, so a slow old search never replaces a new one
	const { filterQuery } = urlFilters;
	React.useEffect(() => {
		let isOutdated = false;
		adminApi
			.listProducts(Object.fromEntries(new URLSearchParams(filterQuery)))
			.then((loadedPage) => {
				if (isOutdated) return;
				setProductPage(loadedPage);
				setLoadError('');
			})
			.catch((error) => !isOutdated && setLoadError(error.message));
		return () => {
			isOutdated = true;
		};
	}, [filterQuery, reloadCount]);

	// Swaps in the product the server answered and reloads the shop so a shown or hidden product appears there at once
	const replaceProduct = (updatedProduct) => {
		setProductPage((currentPage) => ({
			...currentPage,
			items: currentPage.items.map((product) => (product.id === updatedProduct.id ? updatedProduct : product)),
		}));
		reloadShopCatalog();
	};

	// A new product can land on any page, so the table goes back to every product on page 1
	const reloadFromFirstPage = () => {
		urlFilters.clearFilters();
		setReloadCount((count) => count + 1);
		reloadShopCatalog();
	};

	return {
		...urlFilters,
		products: productPage.items,
		totalCount: productPage.totalCount,
		totalPages: productPage.totalPages,
		loadError,
		replaceProduct,
		reloadFromFirstPage,
	};
}
