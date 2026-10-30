// Prices come from the API in Egyptian pounds, the currency Fawaterak charges in.
export function formatPrice(amount) {
	return `EGP ${Number(amount).toLocaleString('en-US', { maximumFractionDigits: 2 })}`;
}

// The last 8 characters are random, the first ones are the time and repeat for orders placed close together
export function shortId(id) {
	return id.slice(-8).toUpperCase();
}

export function formatDate(value) {
	return new Date(value).toLocaleString();
}
