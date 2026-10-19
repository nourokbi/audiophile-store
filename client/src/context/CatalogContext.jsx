import React from 'react';
import { api } from '../lib/api';
import pictures from '../data/pictures.json';

// The products from the API, with the photos and gallery from pictures.json added by slug
const CatalogContext = React.createContext(null);

const sameImageEverywhere = (url) => ({ mobile: url, tablet: url, desktop: url });

function toProduct(p) {
	const look = pictures[p.slug] ?? {};
	const image = look.image ?? p.imageUrl;
	return {
		id: p.id,
		slug: p.slug,
		name: p.name,
		description: p.description,
		features: p.features,
		price: p.price,
		stock: p.stock,
		inStock: p.inStock,
		category: p.category.toLowerCase(),
		includes: Object.entries(p.inTheBox).map(([item, quantity]) => ({ item, quantity })),
		image: typeof image === 'string' ? sameImageEverywhere(image) : image,
		gallery: look.gallery ?? null,
		isNew: Boolean(look.new),
	};
}

export function CatalogProvider({ children }) {
	const [products, setProducts] = React.useState([]);
	const [loading, setLoading] = React.useState(true);
	const [error, setError] = React.useState(null);

	React.useEffect(() => {
		api('/products?pageSize=50')
			.then((page) => setProducts(page.items.map(toProduct)))
			.catch((e) => setError(e.message))
			.finally(() => setLoading(false));
	}, []);

	const value = React.useMemo(
		() => ({ products, loading, error, findBySlug: (slug) => products.find((p) => p.slug === slug) }),
		[products, loading, error],
	);

	return <CatalogContext.Provider value={value}>{children}</CatalogContext.Provider>;
}

export function useCatalog() {
	return React.useContext(CatalogContext);
}
