// Sample numbers for the admin dashboard until the admin API exists, replace each one with an API call later
export const SAMPLE_STATS = {
	revenue: 128450,
	revenueChange: 14.2,
	ordersThisWeek: 38,
	activeOrders: 12,
	customers: 214,
};

export const SAMPLE_SALES_BY_DAY = [
	{ day: 'Mon', orders: 4 },
	{ day: 'Tue', orders: 7 },
	{ day: 'Wed', orders: 9 },
	{ day: 'Thu', orders: 5 },
	{ day: 'Fri', orders: 8 },
	{ day: 'Sat', orders: 11 },
	{ day: 'Sun', orders: 6 },
];

export const SAMPLE_ORDERS = [
	{ id: 'B3D92A2B', customer: 'Mona Hassan', date: '2026-10-19', total: 3848, status: 'Confirmed', method: 'Card' },
	{ id: 'A81F0C3E', customer: 'Omar Adel', date: '2026-10-19', total: 899, status: 'Pending', method: 'Card' },
	{ id: '7C55E901', customer: 'Sara Nabil', date: '2026-10-18', total: 6499, status: 'Processing', method: 'Cash on delivery' },
	{ id: 'F09A6B21', customer: 'Youssef Samir', date: '2026-10-18', total: 2849, status: 'Shipped', method: 'Card' },
	{ id: '2E4D7A10', customer: 'Laila Mostafa', date: '2026-10-17', total: 1349, status: 'Delivered', method: 'Card' },
	{ id: '9B1C3F44', customer: 'Karim Fathy', date: '2026-10-16', total: 4500, status: 'Cancelled', method: 'Cash on delivery' },
];
