import React from 'react';

import { addressesApi } from '../lib/api/addresses';

// The customer's saved addresses for checkout and settings, null while they load
export function useAddresses() {
	const [addresses, setAddresses] = React.useState(null);
	const [loadError, setLoadError] = React.useState('');

	React.useEffect(() => {
		addressesApi
			.list()
			.then(setAddresses)
			.catch((error) => {
				setAddresses([]);
				setLoadError(error.message);
			});
	}, []);

	const addAddress = async (address) => {
		const createdAddress = await addressesApi.create(address);
		setAddresses((currentAddresses) => [...currentAddresses, createdAddress]);
		return createdAddress;
	};

	const removeAddress = async (id) => {
		await addressesApi.remove(id);
		setAddresses((currentAddresses) => currentAddresses.filter((address) => address.id !== id));
	};

	return { addresses, loadError, addAddress, removeAddress };
}
