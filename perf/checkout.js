// Many customers buying the same few products at once, so the stock check, its one retry and the order events are under
// pressure. Results and how to run it are in docs/performance.md
import http from 'k6/http';
import { check, fail, sleep } from 'k6';
import { Counter } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080/api';
const VUS = Number(__ENV.VUS || 20);
const THINK_SECONDS = Number(__ENV.THINK_SECONDS ?? 1); // 0 makes every customer buy nonstop
const HOT_PRODUCTS = Number(__ENV.HOT_PRODUCTS || 1); // 1 puts every customer on the same stock row, the worst case

// A 409 is the API saying the stock changed twice under it, the customer is asked to try again and nothing is lost
const stockConflicts = new Counter('stock_conflicts');
const ordersPlaced = new Counter('orders_placed');

export const options = {
	stages: [
		{ duration: '30s', target: VUS },
		{ duration: '1m', target: VUS },
		{ duration: '15s', target: 0 },
	],
	thresholds: {
		http_req_failed: ['rate<0.01'],
		'http_req_duration{step:checkout}': ['p(95)<1000'],
		'http_req_duration{step:cart}': [],
		'http_req_duration{step:order}': [],
		stock_conflicts: [],
		orders_placed: [],
	},
	summaryTrendStats: ['avg', 'med', 'p(95)', 'p(99)', 'max'],
	setupTimeout: '2m',
};

const json = (token) => ({
	headers: { 'Content-Type': 'application/json', ...(token && { Authorization: `Bearer ${token}` }) },
});

function post(path, body, token, params = {}) {
	return http.post(`${BASE_URL}${path}`, JSON.stringify(body), { ...json(token), ...params });
}

// Creates the products everyone buys and one customer per shopper, all marked with this run's time
export function setup() {
	if (!__ENV.ADMIN_EMAIL || !__ENV.ADMIN_PASSWORD) fail('Set Admin__Email and Admin__Password in .env, the test logs in as the admin');

	const run = Date.now();
	const login = post('/auth/login', { email: __ENV.ADMIN_EMAIL, password: __ENV.ADMIN_PASSWORD });
	if (login.status !== 200) fail(`The admin login answered ${login.status}`);
	const adminToken = login.json('accessToken');

	const productIds = [];
	for (let n = 1; n <= HOT_PRODUCTS; n++) {
		const product = post(
			'/admin/products',
			{
				name: `Load test speaker ${n} ${run}`,
				description: 'Made by perf/checkout.js and hidden again when the test ends.',
				price: 1000,
				stock: 1000000,
				category: 'Speakers',
			},
			adminToken,
		);
		if (product.status !== 201) fail(`Creating a product answered ${product.status}, ${product.body}`);
		productIds.push(product.json('id'));
	}

	const customers = [];
	for (let i = 1; i <= VUS; i++) {
		const register = post('/auth/register', { name: `Load Test ${i}`, email: `k6-${run}-${i}@load.test`, password: 'load-test-1' });
		if (register.status !== 201) fail(`Registering answered ${register.status}, is RATE_LIMITING=false set on the api?`);
		const token = register.json('accessToken');

		const address = post(
			'/addresses',
			{ fullAddressName: `Load Test ${i}`, street: '1 Test Street', city: 'Cairo', country: 'Egypt', phone: '+201000000000' },
			token,
		);
		if (address.status !== 201) fail(`Saving an address answered ${address.status}`);
		customers.push({ token, addressId: address.json('id') });
	}

	return { adminToken, productIds, customers };
}

// One purchase: put a product in the cart, check out with cash on delivery, open the order
export default function ({ productIds, customers }) {
	const { token, addressId } = customers[(__VU - 1) % customers.length];
	const productId = productIds[Math.floor(Math.random() * productIds.length)];

	const cart = post('/cart/items', { productId, quantity: 1 }, token, { tags: { step: 'cart' } });
	check(cart, { 'added to the cart': (r) => r.status === 200 });
	sleep(THINK_SECONDS);

	const checkout = post('/orders', { addressId, paymentMethod: 'CashOnDelivery' }, token, {
		headers: { ...json(token).headers, 'Idempotency-Key': `${__VU}-${__ITER}-${Date.now()}` },
		tags: { step: 'checkout' },
		responseCallback: http.expectedStatuses(201, 409),
	});
	check(checkout, { 'order placed or asked to retry': (r) => r.status === 201 || r.status === 409 });

	if (checkout.status === 201) {
		ordersPlaced.add(1);
		const order = http.get(`${BASE_URL}/orders/${checkout.json('id')}`, { ...json(token), tags: { step: 'order' } });
		check(order, { 'order opens': (r) => r.status === 200 });
	} else if (checkout.status === 409) {
		stockConflicts.add(1);
	}
	sleep(THINK_SECONDS);
}

// Hides the test products, so the shop stops showing them, the orders stay for the dashboard
export function teardown({ adminToken, productIds }) {
	for (const id of productIds) {
		http.put(`${BASE_URL}/admin/products/${id}/availability`, JSON.stringify({ isActive: false }), json(adminToken));
	}
}
