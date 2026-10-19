import React from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { ArrowLeft, CircleCheck, CircleX, CreditCard, Loader, XCircle } from 'lucide-react';

import { OrderItems, OrderProgress, StatusBadge } from '../components/OrderParts';
import { api } from '../lib/api';
import { formatPrice, shortId } from '../lib/format';

// While a card order waits for Fawaterak's webhook this page asks the API every few seconds
const POLL_MS = 3000;
const POLL_FOR_MS = 3 * 60 * 1000;

const OrderDetail = () => {
	const { id } = useParams();
	const [params] = useSearchParams();
	const result = params.get('result'); // success, fail or pending from Fawaterak's redirect
	const [order, setOrder] = React.useState(null);
	const [error, setError] = React.useState('');
	const [timedOut, setTimedOut] = React.useState(false);
	const [cancelling, setCancelling] = React.useState(false);
	const [cancelError, setCancelError] = React.useState('');

	const waitingForPayment =
		order?.status === 'Pending' && order.paymentMethod === 'Card' && order.payment?.status === 'Pending';

	React.useEffect(() => {
		api(`/orders/${id}`)
			.then(setOrder)
			.catch((e) => setError(e.status === 404 ? 'We could not find this order.' : e.message));
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
				// A missed check is fine, the next one tries again
			}
		}, POLL_MS);
		return () => clearInterval(timer);
	}, [id, waitingForPayment]);

	if (error || !order) {
		return (
			<div className='page py-16'>
				<div className='card p-10 text-center'>
					<p className='text-muted'>{error || 'Loading your order...'}</p>
					{error && (
						<Link to='/orders' className='btn-secondary mt-6'>
							My orders
						</Link>
					)}
				</div>
			</div>
		);
	}

	// The same rule as the API, a paid card order needs support because it needs a refund
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
		<div className='page flex flex-col gap-8 pb-24 pt-10'>
			<Link to='/orders' className='label flex items-center gap-2 text-muted hover:text-brand-dark'>
				<ArrowLeft size={16} /> My orders
			</Link>

			<div className='grid grid-cols-1 items-start gap-8 lg:grid-cols-3'>
				<div className='flex flex-col gap-6 lg:col-span-2'>
					<section className='card flex flex-col gap-6 p-6 md:p-8'>
						<Headline order={order} result={result} waiting={waitingForPayment} timedOut={timedOut} />
						{order.status !== 'Cancelled' && <OrderProgress status={order.status} />}
					</section>

					<section className='card flex flex-col gap-4 p-6 md:p-8'>
						<h2 className='text-[18px]'>Items</h2>
						<OrderItems items={order.items} />
					</section>
				</div>

				<aside className='card flex flex-col gap-4 p-6'>
					<div className='flex items-center justify-between'>
						<h2 className='text-[18px]'>Summary</h2>
						<StatusBadge status={order.status} />
					</div>
					<p className='text-[13px] text-muted'>
						Order #{shortId(order.id)} - {new Date(order.createdAt).toLocaleString()}
					</p>
					<div className='flex flex-col gap-2 rounded bg-panel p-4 text-[15px]'>
						<Row label='Subtotal' value={formatPrice(order.subTotal)} />
						<Row label='Shipping' value={order.shippingFee > 0 ? formatPrice(order.shippingFee) : 'Free'} />
						<div className='mt-2 flex items-center justify-between border-t border-line pt-3'>
							<span className='label'>Total</span>
							<span className='text-[22px] font-bold text-brand'>{formatPrice(order.totalAmount)}</span>
						</div>
					</div>
					<p className='flex items-center gap-2 text-[14px] text-muted'>
						<CreditCard size={16} />
						{order.paymentMethod === 'Card' ? 'Card through Fawaterak' : 'Cash on delivery'}
						{order.payment && ` - payment ${order.payment.status.toLowerCase()}`}
					</p>
					{canCancel && (
						<button onClick={cancelOrder} disabled={cancelling} className='btn-secondary w-full'>
							<XCircle size={16} />
							{cancelling ? 'Cancelling...' : 'Cancel order'}
						</button>
					)}
					{cancelError && <p className='text-[14px] font-bold text-red-600'>{cancelError}</p>}
					<Link to='/' className='btn-primary w-full'>
						Continue shopping
					</Link>
				</aside>
			</div>
		</div>
	);
};

const Headline = ({ order, result, waiting, timedOut }) => {
	if (order.status === 'Cancelled') {
		return (
			<Message icon={CircleX} tone='text-red-600' title='Order cancelled'>
				Its items went back into stock.
				{order.payment?.status === 'Succeeded'
					? ' Your payment arrived after the cancellation, so it will be refunded.'
					: ' Nothing was charged.'}
			</Message>
		);
	}

	if (waiting) {
		const failed = result === 'fail';
		const payUrl = order.payment?.checkoutUrl;
		return (
			<Message
				icon={failed ? CircleX : Loader}
				tone={failed ? 'text-red-600' : 'text-brand animate-spin'}
				title={failed ? 'Your payment did not go through' : 'Confirming your payment'}
			>
				{failed
					? 'Nothing was charged. Your items stay reserved for 30 minutes, so you can try again.'
					: timedOut
						? 'Fawaterak has not confirmed this payment yet. If you paid, refresh this page in a minute.'
						: 'We are waiting for Fawaterak to confirm the payment. This page updates by itself.'}
				{payUrl && result !== 'success' && (
					<a href={payUrl} className='btn-primary mt-4'>
						{failed ? 'Try paying again' : 'Go to the payment page'}
					</a>
				)}
			</Message>
		);
	}

	return (
		<Message icon={CircleCheck} tone='text-brand' title='Thank you for your order'>
			{order.paymentMethod === 'Card'
				? 'Your payment is confirmed. We will start preparing your order.'
				: 'Pay in cash when the courier arrives. We will start preparing your order.'}
		</Message>
	);
};

const Message = ({ icon: Icon, tone, title, children }) => (
	<div className='flex gap-4'>
		<Icon size={32} className={`shrink-0 ${tone}`} />
		<div className='flex flex-col items-start gap-1'>
			<h1 className='text-[24px] md:text-[28px]'>{title}</h1>
			<div className='flex flex-col items-start text-[15px] text-muted'>{children}</div>
		</div>
	</div>
);

const Row = ({ label, value }) => (
	<div className='flex items-center justify-between'>
		<span className='text-muted'>{label}</span>
		<span className='font-bold'>{value}</span>
	</div>
);

export default OrderDetail;
