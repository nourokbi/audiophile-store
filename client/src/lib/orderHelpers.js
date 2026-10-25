// The order rules the pages share, they mirror Order.cs in the API so the buttons match what the API allows
const CANCELLABLE_STATUSES = ['Pending', 'Confirmed', 'Processing'];
const FINISHED_STATUSES = ['Delivered', 'Cancelled'];

export const PAYMENT_LABELS = { Card: 'Card', CashOnDelivery: 'Cash on delivery' };

export const isActiveOrder = (order) => !FINISHED_STATUSES.includes(order.status);

// A paid card order needs a refund, so only support can cancel it
export const canCancel = (order) => CANCELLABLE_STATUSES.includes(order.status) && order.payment?.status !== 'Succeeded';

// A card order still waiting for Fawaterak's webhook to say it was paid
export const isWaitingForPayment = (order) =>
	order.status === 'Pending' && order.paymentMethod === 'Card' && order.payment?.status === 'Pending';

// Every status in the order an order moves through, for the admin tabs
export const ORDER_STATUSES = ['Pending', 'Confirmed', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];

// The button text for moving an order to each status, the API's nextStatuses decides which buttons show
export const STATUS_ACTIONS = {
	Confirmed: 'Confirm',
	Processing: 'Start preparing',
	Shipped: 'Mark shipped',
	Delivered: 'Mark delivered',
	Cancelled: 'Cancel order',
};

// The next step forward for the quick button in the orders table, cancelling always needs the order page
export const nextStepForward = (order) => order.nextStatuses.find((status) => status !== 'Cancelled');
