// Sample orders shaped like the planned GET /admin/orders answers, served by a pretend API until Step 2 builds the real one
// Step 2 deletes this file and useAdminOrders, useAdminOrder and the overview import adminApi instead

const PRODUCTS = {
	studio: {
		id: 'b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b01',
		name: 'Studio Reference Headphones',
		price: 3299,
		image: 'studio-reference-headphones',
	},
	planar: {
		id: 'b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b02',
		name: 'Planar Pro Headphones',
		price: 5299,
		image: 'planar-pro-headphones',
	},
	bronze: {
		id: 'b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b03',
		name: 'Bronze Edition Headphones',
		price: 2799,
		image: 'bronze-edition-headphones',
	},
	carbon: {
		id: 'b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b07',
		name: 'Carbon Desk Monitors',
		price: 2899,
		image: 'carbon-desk-monitors',
	},
	cylinder: {
		id: 'b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b08',
		name: 'Cylinder Home Speaker',
		price: 1899,
		image: 'cylinder-home-speaker',
	},
	dac: { id: 'b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b13', name: 'Desktop DAC One', price: 2499, image: 'desktop-dac-one' },
	cable: {
		id: 'b3c1a6e2-6f4d-4c1e-9a51-2f0f6f7c0b15',
		name: 'Silver Balanced Cable',
		price: 1199,
		image: 'silver-balanced-cable',
	},
};

// What the API's Order.cs allows, only this pretend server knows it, the pages read nextStatuses from each order
const NEXT_STATUSES = {
	Pending: ['Confirmed', 'Cancelled'],
	Confirmed: ['Processing', 'Cancelled'],
	Processing: ['Shipped', 'Cancelled'],
	Shipped: ['Delivered'],
	Delivered: [],
	Cancelled: [],
};

const CUSTOMERS = [
	{ name: 'Mona Hassan', email: 'mona@example.com', phone: '+20 100 123 4567', street: '12 Tahrir Street', city: 'Cairo' },
	{ name: 'Omar Adel', email: 'omar@example.com', phone: '+20 111 222 3344', street: '5 Corniche Road', city: 'Alexandria' },
	{ name: 'Sara Nabil', email: 'sara@example.com', phone: '+20 122 333 4455', street: '40 Abbas Street', city: 'Giza' },
	{
		name: 'Youssef Samir',
		email: 'youssef@example.com',
		phone: '+20 155 444 5566',
		street: '8 Port Said Street',
		city: 'Mansoura',
	},
	{ name: 'Laila Mostafa', email: 'laila@example.com', phone: '+20 106 555 6677', street: '21 Nile Street', city: 'Aswan' },
	{ name: 'Karim Fathy', email: 'karim@example.com', phone: '+20 109 666 7788', street: '3 Garden City', city: 'Cairo' },
];

// [customer, days ago, status, payment method, payment status, [product, quantity]...]
const ROWS = [
	[0, 0, 'Pending', 'Card', 'Pending', ['planar', 1]],
	[1, 0, 'Pending', 'CashOnDelivery', 'Pending', ['cylinder', 1], ['cable', 2]],
	[2, 1, 'Confirmed', 'Card', 'Succeeded', ['studio', 1], ['dac', 1]],
	[3, 1, 'Processing', 'CashOnDelivery', 'Pending', ['bronze', 1]],
	[4, 2, 'Confirmed', 'CashOnDelivery', 'Pending', ['carbon', 1]],
	[5, 2, 'Shipped', 'Card', 'Succeeded', ['planar', 1], ['cable', 1]],
	[0, 3, 'Delivered', 'Card', 'Succeeded', ['dac', 1]],
	[1, 4, 'Cancelled', 'Card', 'Failed', ['studio', 2]],
	[2, 5, 'Processing', 'Card', 'Succeeded', ['cylinder', 2]],
	[3, 6, 'Delivered', 'CashOnDelivery', 'Succeeded', ['bronze', 1], ['cable', 1]],
	[4, 8, 'Shipped', 'CashOnDelivery', 'Pending', ['carbon', 2]],
	[5, 10, 'Delivered', 'Card', 'Succeeded', ['studio', 1]],
	[0, 13, 'Cancelled', 'CashOnDelivery', 'Pending', ['planar', 1]],
	[1, 16, 'Delivered', 'Card', 'Succeeded', ['dac', 1], ['cylinder', 1]],
];

const SHIPPING_FEE = 50;
const FREE_SHIPPING_FROM = 5000;

function buildOrder([customerIndex, daysAgo, status, paymentMethod, paymentStatus, ...lines], rowIndex) {
	const customer = CUSTOMERS[customerIndex];
	const items = lines.map(([productKey, quantity]) => {
		const product = PRODUCTS[productKey];
		return {
			productId: product.id,
			productName: product.name,
			productImageUrl: `/assets/products/${product.image}.jpg`,
			unitPrice: product.price,
			quantity,
			lineTotal: product.price * quantity,
		};
	});
	const subTotal = items.reduce((sum, item) => sum + item.lineTotal, 0);
	const shippingFee = subTotal >= FREE_SHIPPING_FROM ? 0 : SHIPPING_FEE;
	const createdAt = new Date(Date.now() - daysAgo * 86400000 - rowIndex * 3600000).toISOString();
	return {
		id: `5a${String(rowIndex).padStart(6, '0')}-0000-4000-8000-${String(rowIndex).padStart(12, '0')}`,
		createdAt,
		updatedAt: createdAt,
		customerName: customer.name,
		customerEmail: customer.email,
		status,
		paymentMethod,
		paymentStatus,
		subTotal,
		shippingFee,
		totalAmount: subTotal + shippingFee,
		itemCount: items.reduce((sum, item) => sum + item.quantity, 0),
		shipping: {
			fullName: customer.name,
			phone: customer.phone,
			street: customer.street,
			city: customer.city,
			country: 'Egypt',
			postalCode: null,
		},
		items,
		nextStatuses: NEXT_STATUSES[status],
	};
}

const orders = ROWS.map(buildOrder);

// The list answer leaves out the address and the items, like AdminOrderSummaryDto
const toSummary = (order) => {
	const summary = { ...order };
	delete summary.shipping;
	delete summary.items;
	return summary;
};

const answerLater = (value) => new Promise((resolve) => setTimeout(() => resolve(structuredClone(value)), 250));

const matches = (order, { status, q, from, to }) => {
	const term = q?.trim().toLowerCase();
	const day = order.createdAt.slice(0, 10);
	return (
		(!status || order.status === status) &&
		(!from || day >= from) &&
		(!to || day <= to) &&
		(!term ||
			order.customerName.toLowerCase().includes(term) ||
			order.customerEmail.toLowerCase().includes(term) ||
			order.id.toLowerCase().startsWith(term))
	);
};

// The same functions, names and answers adminApi gets in Step 2
export const sampleOrdersApi = {
	// GET /admin/orders?status=&q=&from=&to=&page=&pageSize=, newest first, statusCounts ignore the status filter so every tab has its number
	listOrders: ({ status, q, from, to, page = 1, pageSize = 10 } = {}) => {
		const inPeriod = orders.filter((order) => matches(order, { q, from, to }));
		const statusCounts = Object.fromEntries(
			Object.keys(NEXT_STATUSES).map((orderStatus) => [
				orderStatus,
				inPeriod.filter((order) => order.status === orderStatus).length,
			]),
		);
		const matching = inPeriod.filter((order) => !status || order.status === status);
		const pageNumber = Number(page);
		const size = Number(pageSize);
		const items = matching.slice((pageNumber - 1) * size, pageNumber * size).map(toSummary);
		return answerLater({
			items,
			page: pageNumber,
			pageSize: size,
			totalCount: matching.length,
			totalPages: Math.max(1, Math.ceil(matching.length / size)),
			statusCounts,
		});
	},

	// GET /admin/orders/{id}
	getOrder: (id) => {
		const order = orders.find((candidate) => candidate.id === id);
		return order ? answerLater(order) : Promise.reject(Object.assign(new Error('Order not found.'), { status: 404 }));
	},

	// PUT /admin/orders/{id}/status with { status }, an illegal move is a 409 like the API
	changeOrderStatus: (id, nextStatus) => {
		const order = orders.find((candidate) => candidate.id === id);
		if (!order) return Promise.reject(Object.assign(new Error('Order not found.'), { status: 404 }));
		if (!order.nextStatuses.includes(nextStatus)) {
			return Promise.reject(
				Object.assign(new Error(`An order that is ${order.status} cannot become ${nextStatus}.`), { status: 409 }),
			);
		}
		order.status = nextStatus;
		order.nextStatuses = NEXT_STATUSES[nextStatus];
		order.updatedAt = new Date().toISOString();
		if (nextStatus === 'Cancelled' && order.paymentStatus === 'Pending') order.paymentStatus = 'Failed';
		return answerLater(order);
	},
};
