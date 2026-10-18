import React from 'react';
import { api } from '../lib/api';

// The API's products, by slug. data.json still drives the look of the pages (images, gallery,
// "you may also like"); the API is the source of truth for what can be bought: id, price, stock.
// The two meet on the slug, which the API seeds with the same values as data.json.
const CatalogContext = React.createContext({ bySlug: {}, loading: true, error: null });

export function CatalogProvider({ children }) {
	const [state, setState] = React.useState({ bySlug: {}, loading: true, error: null });

	React.useEffect(() => {
		api('/products?pageSize=50')
			.then((page) =>
				setState({
					bySlug: Object.fromEntries(page.items.map((p) => [p.slug, p])),
					loading: false,
					error: null,
				}),
			)
			.catch((e) => setState({ bySlug: {}, loading: false, error: e.message }));
	}, []);

	return <CatalogContext.Provider value={state}>{children}</CatalogContext.Provider>;
}

export function useCatalog() {
	return React.useContext(CatalogContext);
}
