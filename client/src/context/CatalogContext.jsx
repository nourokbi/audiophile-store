import React from 'react';
import { api } from '../lib/api';
import pictures from '../data/pictures.json';

// The products from the API, with the photos and gallery from pictures.json added by slug
const CatalogContext = React.createContext(null);

const sameImageEverywhere = (url) => ({ mobile: url, tablet: url, desktop: url });

// A plain grey square for a product added without a photo, so every page can read image.mobile
const NO_IMAGE = 'data:image/svg+xml,' + encodeURIComponent("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1 1'><rect width='1' height='1' fill='#d4d4d4'/></svg>");

export function toProduct(p) {
	const look = pictures[p.slug] ?? {};
	const image = look.image ?? p.imageUrl ?? NO_IMAGE;
	return {
		id: p.id,
		slug: p.slug,
		name: p.name,
		description: p.description,
		features: p.features,
		price: p.price,
		stock: p.stock,
		inStock: p.inStock,
		isActive: p.isActive,
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

	// Loads the shop's products, the admin dashboard calls it again after a change so the shop shows it without a refresh
	const reload = React.useCallback(
		() =>
			api('/products?pageSize=50')
				.then((page) => setProducts(page.items.map(toProduct)))
				.catch((e) => setError(e.message))
				.finally(() => setLoading(false)),
		[],
	);

	React.useEffect(() => {
		reload();
	}, [reload]);

	const value = React.useMemo(
		() => ({ products, loading, error, reload, findBySlug: (slug) => products.find((p) => p.slug === slug) }),
		[products, loading, error, reload],
	);

	return <CatalogContext.Provider value={value}>{children}</CatalogContext.Provider>;
}

export function useCatalog() {
	return React.useContext(CatalogContext);
}
