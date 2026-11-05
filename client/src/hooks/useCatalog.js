import { keepPreviousData, useQuery } from '@tanstack/react-query';

import { productsApi } from '../lib/api/products';
import { queryKeys } from '../lib/queryClient';

const RELATED_COUNT = 3;

// One page of the shop, the page on screen stays until the next one arrives so the grid never flashes empty
export function useCatalogPage(query, enabled) {
	return useQuery({
		queryKey: queryKeys.catalogPage(query),
		queryFn: () => productsApi.listPage(query),
		placeholderData: keepPreviousData,
		enabled,
	});
}

// One product by its address, a product that does not exist is not worth asking for twice
export function useCatalogProduct(slug) {
	return useQuery({
		queryKey: queryKeys.catalogProduct(slug),
		queryFn: () => productsApi.getBySlug(slug),
		retry: (failureCount, error) => error.status !== 404 && failureCount < 1,
	});
}

export function useRelatedProducts(productId) {
	const { data: products = [] } = useQuery({
		queryKey: queryKeys.relatedProducts(productId),
		queryFn: () => productsApi.listRelated(productId, RELATED_COUNT),
		enabled: Boolean(productId),
	});
	return products;
}
