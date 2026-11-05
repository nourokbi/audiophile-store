// What every visitor does: open the shop, open a product, search. Results and how to run it are in docs/performance.md
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080/api';
const VUS = Number(__ENV.VUS || 50);
const THINK_SECONDS = Number(__ENV.THINK_SECONDS ?? 1); // 0 makes every shopper click nonstop, to find the API's limit
const SEARCH_TERMS = ['headphones', 'speaker', 'earphones', 'wireless', 'studio'];

export const options = {
	stages: [
		{ duration: '30s', target: VUS }, // shoppers arrive
		{ duration: '1m', target: VUS }, // the steady part
		{ duration: '15s', target: 0 }, // shoppers leave
	],
	// An empty filter makes k6 report each page's own numbers, the limits fail the run when the API can't keep up
	thresholds: {
		http_req_failed: ['rate<0.01'],
		http_req_duration: ['p(95)<500'],
		'http_req_duration{page:shop}': [],
		'http_req_duration{page:product}': [],
		'http_req_duration{page:search}': [],
	},
	summaryTrendStats: ['avg', 'med', 'p(95)', 'p(99)', 'max'],
};

function get(path, page) {
	const response = http.get(`${BASE_URL}${path}`, { tags: { page } });
	check(response, { 'status is 200': (r) => r.status === 200 });
	return response;
}

// Runs once before the shoppers start and reads real product ids from the API
export function setup() {
	const ids = http.get(`${BASE_URL}/products?pageSize=50`).json('items').map((product) => product.id);
	if (ids.length === 0) throw new Error('The API returned no products');
	return { ids };
}

// One shopper's visit, the sleeps are the time a person spends reading the page
export default function ({ ids }) {
	// The shop page loads the whole catalogue 50 at a time, like client/src/lib/api/products.js
	get('/products?pageSize=50&page=1', 'shop');
	get('/products?pageSize=50&page=2', 'shop');
	sleep(THINK_SECONDS);

	get(`/products/${ids[Math.floor(Math.random() * ids.length)]}`, 'product');
	sleep(THINK_SECONDS);

	get(`/products/search?q=${SEARCH_TERMS[Math.floor(Math.random() * SEARCH_TERMS.length)]}`, 'search');
	sleep(THINK_SECONDS);
}
