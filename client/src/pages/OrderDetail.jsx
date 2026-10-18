import React from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';

import successLogo from '/assets/checkout/icon-order-confirmation.svg';
import { api } from '../lib/api';
import { formatPrice, shortId } from '../lib/format';

// The order progress page. After paying on Fawaterak the browser lands here
// (Fawaterak → /payments/fawaterak/return → /orders?orderId=… → /orders/:id).
// The redirect itself proves nothing, so while the order is Pending this page asks the API
// every few seconds; it turns Confirmed when Fawaterak's webhook reaches the API.
const POLL_MS = 3000;
const POLL_FOR_MS = 3 * 60 * 1000;

const STEPS = [
	{ status: 'Pending', label: 'Placed' },
	{ status: 'Confirmed', label: 'Confirmed' },
	{ status: 'Processing', label: 'Preparing' },
	{ status: 'Shipped', label: 'Shipped' },
	{ status: 'Delivered', label: 'Delivered' },
];

const OrderDetail = () => {
	const { id } = useParams();
	const [params] = useSearchParams();
	const result = params.get('result'); // success | fail | pending, from Fawaterak's redirect
	const [order, setOrder] = React.useState(null);
	const [error, setError] = React.useState('');
	const [timedOut, setTimedOut] = React.useState(false);
	const [cancelling, setCancelling] = React.useState(false);
	const [cancelError, setCancelError] = React.useState('');

	const waitingForPayment =
		order?.status === 'Pending' && order.paymentMethod === 'Card' && order.payment?.status === 'Pending';

	React.useEffect(() => {
		let stop = false;
		const load = () =>
			api(`/orders/${id}`)
				.then((o) => !stop && setOrder(o))
				.catch((e) => !stop && setError(e.status === 404 ? 'We could not find this order.' : e.message));
		load();
		return () => {
			stop = true;
		};
	}, [id]);

	React.useEffect(() => {
		if (!waitingForPayment) return;
		const started = Date.now();
		const timer = setInterval(async () => {
			if (Date.now() - started > POLL_FOR_MS) {
				clearInterval(timer);
				setTimedOut(true);
				return;
			}
			try {
				setOrder(await api(`/orders/${id}`));
			} catch {
				// A missed poll is fine; the next one tries again.
			}
		}, POLL_MS);
		return () => clearInterval(timer);
	}, [id, waitingForPayment]);

	if (error) {
		return (
			<Shell>
				<h3>Order not found</h3>
				<p className='p text-black/50 mt-4'>{error}</p>
				<Link to='/orders'>
					<button className='btn bg-cream hover:bg-cream-light mt-8'>My orders</button>
				</Link>
			</Shell>
		);
	}

	if (!order) {
		return (
			<Shell>
				<p className='p text-black/50'>Loading your order…</p>
			</Shell>
		);
	}

	const cancelled = order.status === 'Cancelled';
	const currentStep = STEPS.findIndex((s) => s.status === order.status);

	// The same rule as the API, a paid card order is cancelled by support because it needs a refund
	const canCancel =
		['Pending', 'Confirmed', 'Processing'].includes(order.status) && order.payment?.status !== 'Succeeded';

	const cancelOrder = async () => {
		if (!window.confirm('Cancel this order? Its items go back into stock.')) return;
		setCancelling(true);
		setCancelError('');
		try {
			setOrder(await api(`/orders/${id}/cancel`, { method: 'POST' }));
		} catch (e) {
			setCancelError(e.message);
		} finally {
			setCancelling(false);
		}
	};

	return (
		<Shell>
			<Headline order={order} result={result} waiting={waitingForPayment} timedOut={timedOut} />

			{/* Progress */}
			{!cancelled && (
				<ol className='mt-10 grid xs:grid-cols-1 md:grid-cols-5 gap-3'>
					{STEPS.map((step, i) => {
						const done = i <= currentStep;
						return (
							<li
								key={step.status}
								className={`rounded-lg px-4 py-3 border ${
									done ? 'bg-cream border-cream text-white' : 'bg-white border-stone-300 text-black/40'
								}`}
							>
								<p className='text-[11px] uppercase tracking-[1px] font-bold opacity-80'>Step {i + 1}</p>
								<p className='text-[15px] font-bold'>{step.label}</p>
							</li>
						);
					})}
				</ol>
			)}

			{/* Items and totals, in the success modal's two-tone layout */}
			<div className='mt-10 flex xs:flex-col md:flex-row w-full rounded-lg overflow-hidden'>
				<div className='bg-gray-light flex-[3] flex flex-col gap-4 p-6'>
					{order.items.map((item) => (
						<div key={item.productId} className='flex items-center justify-between gap-4'>
							<div className='flex items-center gap-4 min-w-0'>
								{item.productImageUrl && (
									<img src={item.productImageUrl} alt='' className='w-16 h-16 rounded-[8px] shrink-0' />
								)}
								<div className='min-w-0'>
									<p className='text-[15px] font-bold leading-[25px] text-black truncate'>{item.productName}</p>
									<p className='text-[14px] font-bold leading-[25px] text-black/50'>{formatPrice(item.unitPrice)}</p>
								</div>
							</div>
							<p className='text-[15px] font-bold text-black/50 shrink-0'>x{item.quantity}</p>
						</div>
					))}
				</div>
				<div className='bg-stone flex-[2] flex flex-col justify-center gap-3 p-6'>
					<Row label='Subtotal' value={formatPrice(order.subTotal)} />
					<Row label='Shipping' value={order.shippingFee > 0 ? formatPrice(order.shippingFee) : 'Free'} />
					<div className='border-t border-white/10 pt-3'>
						<p className='text-[15px] font-[500] leading-[25px] text-white/50 uppercase'>Grand total</p>
						<h6 className='font-bold leading-[25px] text-white'>{formatPrice(order.totalAmount)}</h6>
					</div>
				</div>
			</div>

			<div className='mt-6 flex xs:flex-col md:flex-row gap-4 items-start md:items-center justify-between'>
				<p className='text-[14px] text-black/50'>
					Order #{shortId(order.id)} · {order.paymentMethod === 'Card' ? 'Card (Fawaterak)' : 'Cash on delivery'} · placed{' '}
					{new Date(order.createdAt).toLocaleString()}
				</p>
				<div className='flex xs:flex-col md:flex-row gap-4'>
					{canCancel && (
						<button
							className='btn border border-black text-black bg-white hover:bg-black hover:text-white disabled:opacity-50'
							onClick={cancelOrder}
							disabled={cancelling}
						>
							{cancelling ? 'Cancelling…' : 'Cancel order'}
						</button>
					)}
					<Link to='/orders'>
						<button className='btn bg-stone hover:bg-black/70'>My orders</button>
					</Link>
					<Link to='/'>
						<button className='btn bg-cream hover:bg-cream-light'>Continue shopping</button>
					</Link>
				</div>
			</div>
			{cancelError && <p className='mt-4 text-[14px] font-bold text-red-600'>{cancelError}</p>}
		</Shell>
	);
};

const Headline = ({ order, result, waiting, timedOut }) => {
	const payUrl = order.payment?.checkoutUrl;

	if (order.status === 'Cancelled') {
		return (
			<>
				<h3>Order cancelled</h3>
				<p className='p text-black/50 mt-4'>
					This order was cancelled and its items went back into stock.
					{order.payment?.status === 'Succeeded'
						? ' Your payment was received after the cancellation; it will be refunded.'
						: ' Nothing was charged.'}
				</p>
			</>
		);
	}

	if (waiting) {
		const failed = result === 'fail';
		return (
			<>
				<p className='text-cream text-[13px] font-bold uppercase leading-[25px] tracking-wide'>
					{failed ? 'Payment not completed' : 'Waiting for payment'}
				</p>
				<h3 className='mt-2'>{failed ? 'Your payment didn’t go through' : 'Confirming your payment…'}</h3>
				<p className='p text-black/50 mt-4 max-w-[60ch]'>
					{failed
						? 'Nothing was charged. You can try again with the same order: your items are still reserved for 30 minutes.'
						: timedOut
							? 'Fawaterak hasn’t confirmed this payment yet. If you paid, this page will show it once the confirmation arrives; refresh in a minute.'
							: 'We’re waiting for Fawaterak to tell us the payment went through. This page updates by itself.'}
				</p>
				{payUrl && (failed || result !== 'success') && (
					<a href={payUrl}>
						<button className='btn bg-cream hover:bg-cream-light mt-8'>
							{failed ? 'Try paying again' : 'Go to the payment page'}
						</button>
					</a>
				)}
			</>
		);
	}

	return (
		<>
			<img src={successLogo} alt='' />
			<h3 className='xs:mt-[23px] md:mt-[33px]'>Thank you for your order</h3>
			<p className='p text-black/50 xs:mt-4 md:mt-6'>
				{order.paymentMethod === 'Card'
					? 'Your payment is confirmed. We’ll start preparing your order.'
					: 'Pay in cash when the courier arrives. We’ll start preparing your order.'}
			</p>
		</>
	);
};

const Row = ({ label, value }) => (
	<div className='flex items-center justify-between'>
		<p className='text-[15px] font-[500] leading-[25px] text-white/50 uppercase'>{label}</p>
		<p className='text-[15px] font-bold text-white'>{value}</p>
	</div>
);

const Shell = ({ children }) => (
	<main className='mt-[95px] bg-gray-light'>
		<div className='xs:px-[24px] md:px-[40px] lg:px-[165px] xs:pt-10 md:pt-16 lg:pt-[79px] pb-[140px]'>
			<section className='bg-white xs:p-6 md:p-12 rounded-[8px] w-full'>{children}</section>
		</div>
	</main>
);

export default OrderDetail;
