import { useQuery } from '@tanstack/react-query';

import { productsApi } from '../lib/api/products';
import { queryKeys } from '../lib/queryClient';

// Every product on sale, loaded once and shared by the shop pages through the cache
export function useCatalog() {
	const { data: products = [], isPending, error } = useQuery({ queryKey: queryKeys.catalog, queryFn: productsApi.listAll });

	return {
		products,
		loading: isPending,
		error: error?.message ?? null,
		findBySlug: (slug) => products.find((product) => product.slug === slug),
	};
}
