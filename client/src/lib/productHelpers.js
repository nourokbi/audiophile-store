import pictures from '../data/pictures.json';

// At or below this the shop warns that only a few are left
export const FEW_LEFT = 5;

// At or below this the dashboard calls it low stock, the same number as ProductQuery.LowStockLimit in the API
export const LOW_STOCK_LIMIT = 8;

const sameImageEverywhere = (url) => ({ mobile: url, tablet: url, desktop: url });

// A plain grey square for a product added without a photo, so every page can read image.mobile
const NO_IMAGE =
	'data:image/svg+xml,' +
	encodeURIComponent(
		"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1 1'><rect width='1' height='1' fill='#d4d4d4'/></svg>",
	);

// The API's product in the shape every page uses, with the photos and gallery from pictures.json added by slug
export function toProduct(apiProduct) {
	const look = pictures[apiProduct.slug] ?? {};
	// The pictures.json photos stay while the API still has the image it was seeded with, a new image saved in the dashboard wins
	const hasNewImage = apiProduct.imageUrl && apiProduct.imageUrl !== look.image && apiProduct.imageUrl !== look.seededImageUrl;
	const image = (hasNewImage ? apiProduct.imageUrl : look.image) ?? apiProduct.imageUrl ?? NO_IMAGE;
	return {
		id: apiProduct.id,
		slug: apiProduct.slug,
		name: apiProduct.name,
		description: apiProduct.description,
		features: apiProduct.features,
		price: apiProduct.price,
		stock: apiProduct.stock,
		inStock: apiProduct.inStock,
		isActive: apiProduct.isActive,
		category: apiProduct.category.toLowerCase(),
		includes: Object.entries(apiProduct.inTheBox).map(([item, quantity]) => ({ item, quantity })),
		image: typeof image === 'string' ? sameImageEverywhere(image) : image,
		imageUrl: apiProduct.imageUrl, // the address saved in the API, the admin edit form shows it
		gallery: look.gallery ?? null,
		isNew: Boolean(look.new),
	};
}

export const productPath = (product) => `/${product.category}/${product.slug}`;

export const isFewLeft = (product) => product.inStock && product.stock <= FEW_LEFT;

// The short stock line on the cards, the product page and the cart
export function stockLabel(product) {
	if (!product.inStock) return 'Out of stock';
	return isFewLeft(product) ? `Only ${product.stock} left` : 'In stock';
}
