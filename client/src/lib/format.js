// Prices come from the API in Egyptian pounds, the currency Fawaterak charges in.
export function formatPrice(amount) {
	return `EGP ${Number(amount).toLocaleString('en-US', { maximumFractionDigits: 2 })}`;
}

export function shortId(id) {
	return id.slice(0, 8).toUpperCase();
}

export function formatDate(value) {
	return new Date(value).toLocaleString();
}
