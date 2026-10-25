import React from 'react';

import { productsApi } from '../lib/api/products';

// Every product on sale, loaded once and shared by the shop pages
const CatalogContext = React.createContext(null);

export function CatalogProvider({ children }) {
	const [products, setProducts] = React.useState([]);
	const [loading, setLoading] = React.useState(true);
	const [error, setError] = React.useState(null);

	// The admin dashboard calls it again after a change so the shop shows it without a refresh
	const reload = React.useCallback(
		() =>
			productsApi
				.listAll()
				.then((loadedProducts) => {
					setProducts(loadedProducts);
					setError(null);
				})
				.catch((loadError) => setError(loadError.message))
				.finally(() => setLoading(false)),
		[],
	);

	React.useEffect(() => {
		reload();
	}, [reload]);

	const value = React.useMemo(
		() => ({
			products,
			loading,
			error,
			reload,
			findBySlug: (slug) => products.find((product) => product.slug === slug),
		}),
		[products, loading, error, reload],
	);

	return <CatalogContext.Provider value={value}>{children}</CatalogContext.Provider>;
}

export function useCatalog() {
	return React.useContext(CatalogContext);
}
