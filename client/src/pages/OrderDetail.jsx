import React from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { ArrowLeft, CreditCard, XCircle } from 'lucide-react';

import OrderHeadline from '../components/orders/OrderHeadline';
import OrderItems from '../components/orders/OrderItems';
import OrderProgress from '../components/orders/OrderProgress';
import StatusBadge from '../components/orders/StatusBadge';
import PriceSummary from '../components/shop/PriceSummary';
import { ordersApi } from '../lib/api/orders';
import { formatDate, shortId } from '../lib/format';
import { canCancel, isWaitingForPayment, PAYMENT_LABELS } from '../lib/orderHelpers';

// While a card order waits for Fawaterak's webhook this page asks the API every few seconds
const CHECK_EVERY_MS = 3000;
const STOP_CHECKING_AFTER_MS = 3 * 60 * 1000;

const OrderDetail = () => {
	const { id } = useParams();
	const [searchParams] = useSearchParams();
	const paymentResult = searchParams.get('result'); // success, fail or pending from Fawaterak's redirect
	const [order, setOrder] = React.useState(null);
	const [loadError, setLoadError] = React.useState('');
	const [hasStoppedWaiting, setHasStoppedWaiting] = React.useState(false);
	const [isCancelling, setIsCancelling] = React.useState(false);
	const [cancelError, setCancelError] = React.useState('');

	const isWaiting = order !== null && isWaitingForPayment(order);

	React.useEffect(() => {
		ordersApi
			.get(id)
			.then(setOrder)
			.catch((error) => setLoadError(error.status === 404 ? 'We could not find this order.' : error.message));
	}, [id]);

	React.useEffect(() => {
		if (!isWaiting) return;
		const startedAt = Date.now();
		const checkTimer = setInterval(async () => {
			if (Date.now() - startedAt > STOP_CHECKING_AFTER_MS) {
				clearInterval(checkTimer);
				setHasStoppedWaiting(true);
				return;
			}
			try {
				setOrder(await ordersApi.get(id));
			} catch {
				// A missed check is fine, the next one tries again
			}
		}, CHECK_EVERY_MS);
		return () => clearInterval(checkTimer);
	}, [id, isWaiting]);

	if (loadError || !order) {
		return (
			<div className='page py-16'>
				<div className='card p-10 text-center'>
					<p className='text-muted'>{loadError || 'Loading your order...'}</p>
					{loadError && (
						<Link to='/orders' className='btn-secondary mt-6'>
							My orders
						</Link>
					)}
				</div>
			</div>
		);
	}

	const cancelOrder = async () => {
		if (!window.confirm('Cancel this order? Its items go back into stock.')) return;
		setIsCancelling(true);
		setCancelError('');
		try {
			setOrder(await ordersApi.cancel(id));
		} catch (error) {
			setCancelError(error.message);
		} finally {
			setIsCancelling(false);
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
						<OrderHeadline
							order={order}
							paymentResult={paymentResult}
							isWaitingForPayment={isWaiting}
							hasStoppedWaiting={hasStoppedWaiting}
						/>
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
						Order #{shortId(order.id)} - {formatDate(order.createdAt)}
					</p>
					<PriceSummary subTotal={order.subTotal} shippingFee={order.shippingFee} total={order.totalAmount} />
					<p className='flex items-center gap-2 text-[14px] text-muted'>
						<CreditCard size={16} />
						{PAYMENT_LABELS[order.paymentMethod]}
						{order.payment && ` - payment ${order.payment.status.toLowerCase()}`}
					</p>
					{canCancel(order) && (
						<button onClick={cancelOrder} disabled={isCancelling} className='btn-secondary w-full'>
							<XCircle size={16} />
							{isCancelling ? 'Cancelling...' : 'Cancel order'}
						</button>
					)}
					{cancelError && <p className='error-text'>{cancelError}</p>}
					<Link to='/' className='btn-primary w-full'>
						Continue shopping
					</Link>
				</aside>
			</div>
		</div>
	);
};

export default OrderDetail;
