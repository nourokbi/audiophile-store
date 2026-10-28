import React from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
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
import { queryKeys } from '../lib/queryClient';

// While a card order waits for Fawaterak's webhook this page asks the API every few seconds, for a few minutes at most
const CHECK_EVERY_MS = 3000;
const STOP_CHECKING_AFTER_MS = 3 * 60 * 1000;

const OrderDetail = () => {
	const { id } = useParams();
	const [searchParams] = useSearchParams();
	const paymentResult = searchParams.get('result'); // success, fail or pending from Fawaterak's redirect
	const queryClient = useQueryClient();
	const [hasStoppedWaiting, setHasStoppedWaiting] = React.useState(false);

	const { data: order = null, error } = useQuery({
		queryKey: queryKeys.myOrder(id),
		queryFn: () => ordersApi.get(id),
		refetchInterval: (query) =>
			query.state.data && isWaitingForPayment(query.state.data) && !hasStoppedWaiting ? CHECK_EVERY_MS : false,
	});
	const loadError = !error ? '' : error.status === 404 ? 'We could not find this order.' : error.message;
	const isWaiting = order !== null && isWaitingForPayment(order);

	React.useEffect(() => {
		if (!isWaiting) return;
		const stopTimer = setTimeout(() => setHasStoppedWaiting(true), STOP_CHECKING_AFTER_MS);
		return () => clearTimeout(stopTimer);
	}, [isWaiting]);

	// After a cancel, my orders (this one included) and the shop's stock load again
	const cancel = useMutation({
		mutationFn: () => ordersApi.cancel(id),
		onSuccess: () =>
			Promise.all([
				queryClient.invalidateQueries({ queryKey: queryKeys.myOrders }),
				queryClient.invalidateQueries({ queryKey: queryKeys.catalog }),
			]),
	});

	// Asks the API for a new payment page and goes there, or shows the order paid if the last attempt went through after all
	const payAgain = useMutation({
		mutationFn: () => ordersApi.retryPayment(id),
		onSuccess: (updatedOrder) => {
			if (updatedOrder.payment?.checkoutUrl) window.location.assign(updatedOrder.payment.checkoutUrl);
			else return queryClient.invalidateQueries({ queryKey: queryKeys.myOrders });
		},
	});

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

	const cancelOrder = () => {
		if (window.confirm('Cancel this order? Its items go back into stock.')) cancel.mutate();
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
							onPayAgain={() => payAgain.mutate()}
							isOpeningPaymentPage={payAgain.isPending}
						/>
						{payAgain.error && <p className='error-text'>{payAgain.error.message}</p>}
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
						<button onClick={cancelOrder} disabled={cancel.isPending} className='btn-secondary w-full'>
							<XCircle size={16} />
							{cancel.isPending ? 'Cancelling...' : 'Cancel order'}
						</button>
					)}
					{cancel.error && <p className='error-text'>{cancel.error.message}</p>}
					<Link to='/' className='btn-primary w-full'>
						Continue shopping
					</Link>
				</aside>
			</div>
		</div>
	);
};

export default OrderDetail;
