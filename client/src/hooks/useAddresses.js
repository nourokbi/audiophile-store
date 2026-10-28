import { useQuery, useQueryClient } from '@tanstack/react-query';

import { addressesApi } from '../lib/api/addresses';
import { queryKeys } from '../lib/queryClient';

// The customer's saved addresses for checkout and settings, null while they load
// A change puts the server's answer straight into the cache, so both pages see it without another request
export function useAddresses() {
	const queryClient = useQueryClient();
	const { data, error } = useQuery({ queryKey: queryKeys.addresses, queryFn: addressesApi.list });

	const addAddress = async (address) => {
		const createdAddress = await addressesApi.create(address);
		queryClient.setQueryData(queryKeys.addresses, (currentAddresses = []) => [...currentAddresses, createdAddress]);
		return createdAddress;
	};

	const removeAddress = async (id) => {
		await addressesApi.remove(id);
		queryClient.setQueryData(queryKeys.addresses, (currentAddresses = []) =>
			currentAddresses.filter((address) => address.id !== id),
		);
	};

	return { addresses: data ?? (error ? [] : null), loadError: error?.message ?? '', addAddress, removeAddress };
}
