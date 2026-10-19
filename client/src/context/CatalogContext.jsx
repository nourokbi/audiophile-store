import React from 'react';
import { api } from '../lib/api';
import data from '../data/data.json';

// One product list for the whole shop, text and images from data.json joined on the slug with the API's id, price and stock
const CatalogContext = React.createContext(null);

// New products first, then the dearest, so the flagship models lead the grid
const LOOKS = [...data].sort((a, b) => Number(b.new) - Number(a.new) || b.price - a.price);

export function CatalogProvider({ children }) {
	const [live, setLive] = React.useState({});
	const [loading, setLoading] = React.useState(true);
	const [error, setError] = React.useState(null);

	React.useEffect(() => {
		api('/products?pageSize=50')
			.then((page) => setLive(Object.fromEntries(page.items.map((p) => [p.slug, p]))))
			.catch((e) => setError(e.message))
			.finally(() => setLoading(false));
	}, []);

	const value = React.useMemo(() => {
		// A product the API does not sell keeps no id or price, so it shows as not available
		const products = LOOKS.map((look) => {
			const sold = live[look.slug];
			return {
				...look,
				id: sold?.id ?? null,
				price: sold?.price ?? null,
				stock: sold?.stock ?? 0,
				inStock: Boolean(sold?.inStock),
			};
		});
		return { products, loading, error, findBySlug: (slug) => products.find((p) => p.slug === slug) };
	}, [live, loading, error]);

	return <CatalogContext.Provider value={value}>{children}</CatalogContext.Provider>;
}

export function useCatalog() {
	return React.useContext(CatalogContext);
}
