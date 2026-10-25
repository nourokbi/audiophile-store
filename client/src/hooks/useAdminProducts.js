import React from 'react';

import { useCatalog } from '../context/CatalogContext';
import { adminApi } from '../lib/adminApi';

const SEARCH_WAIT_MS = 300;

// The page of products the dashboard shows, from the list or from a search, with hidden products included
export function useAdminProducts() {
	const { reload: reloadShopCatalog } = useCatalog();
	const [products, setProducts] = React.useState([]);
	const [currentPage, setCurrentPage] = React.useState(1);
	const [totalPages, setTotalPages] = React.useState(1);
	const [searchText, setSearchText] = React.useState(''); // what is typed in the box
	const [searchTerm, setSearchTerm] = React.useState(''); // what is searched, set once typing stops
	const [reloadCount, setReloadCount] = React.useState(0); // raised to load the same page again
	const [loadError, setLoadError] = React.useState('');

	// Waits until typing stops so the API gets one request instead of one per letter, a new search starts on page 1
	React.useEffect(() => {
		const searchTimer = setTimeout(() => {
			setSearchTerm(searchText.trim());
			setCurrentPage(1);
		}, SEARCH_WAIT_MS);
		return () => clearTimeout(searchTimer);
	}, [searchText]);

	// An answer that arrives after a newer request is ignored, so a slow old search never replaces a new one
	React.useEffect(() => {
		let isOutdated = false;
		const pageRequest = searchTerm
			? adminApi.searchProducts(searchTerm, currentPage)
			: adminApi.listProducts(currentPage);
		pageRequest
			.then((productPage) => {
				if (isOutdated) return;
				setProducts(productPage.items);
				setTotalPages(productPage.totalPages);
				setLoadError('');
			})
			.catch((error) => !isOutdated && setLoadError(error.message));
		return () => {
			isOutdated = true;
		};
	}, [currentPage, searchTerm, reloadCount]);

	// Swaps in the product the server answered and reloads the shop so a shown or hidden product appears there at once
	const replaceProduct = (updatedProduct) => {
		setProducts((currentProducts) =>
			currentProducts.map((product) => (product.id === updatedProduct.id ? updatedProduct : product)),
		);
		reloadShopCatalog();
	};

	// A new product can land on any page, so the table goes back to the full list on page 1
	const reloadFromFirstPage = () => {
		setSearchText('');
		setCurrentPage(1);
		setReloadCount((count) => count + 1);
		reloadShopCatalog();
	};

	return {
		products,
		currentPage,
		totalPages,
		searchText,
		loadError,
		setSearchText,
		setCurrentPage,
		replaceProduct,
		reloadFromFirstPage,
	};
}
