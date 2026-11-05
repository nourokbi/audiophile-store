// What every visitor does: open the shop, open a product, search. Results and how to run it are in docs/performance.md
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080/api';
const VUS = Number(__ENV.VUS || 50);
const THINK_SECONDS = Number(__ENV.THINK_SECONDS ?? 1); // 0 makes every shopper click nonstop, to find the API's limit
const SEARCH_TERMS = ['headphones', 'speaker', 'earphones', 'wireless', 'studio'];
const PAGE_SIZE = 9; // what the shop asks for by default

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

const pick = (list) => list[Math.floor(Math.random() * list.length)];

// Runs once before the shoppers start and reads real products and the page count from the API
export function setup() {
	const firstPage = http.get(`${BASE_URL}/products?pageSize=${PAGE_SIZE}`).json();
	if (firstPage.items.length === 0) throw new Error('The API returned no products');
	return { products: firstPage.items.map(({ id, slug }) => ({ id, slug })), totalPages: firstPage.totalPages };
}

// One shopper's visit, the sleeps are the time a person spends reading the page
export default function ({ products, totalPages }) {
	// The first page half the time, like most visits, otherwise any page of the catalogue
	const page = Math.random() < 0.5 ? 1 : Math.ceil(Math.random() * totalPages);
	get(`/products?sort=Featured&pageSize=${PAGE_SIZE}&page=${page}`, 'shop');
	sleep(THINK_SECONDS);

	const product = pick(products);
	get(`/products/by-slug/${product.slug}`, 'product');
	get(`/products/${product.id}/related?count=3`, 'product');
	sleep(THINK_SECONDS);

	get(`/products?q=${pick(SEARCH_TERMS)}&sort=Featured&pageSize=${PAGE_SIZE}`, 'search');
	sleep(THINK_SECONDS);
}
