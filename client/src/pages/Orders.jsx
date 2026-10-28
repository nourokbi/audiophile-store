import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, Navigate, useSearchParams } from 'react-router-dom';
import { ArrowRight, ShoppingBag, User } from 'lucide-react';

import OrderItems from '../components/orders/OrderItems';
import OrderProgress from '../components/orders/OrderProgress';
import StatusBadge from '../components/orders/StatusBadge';
import EmptyState from '../components/ui/EmptyState';
import { useAuth } from '../context/AuthContext';
import { ordersApi } from '../lib/api/orders';
import { formatDate, formatPrice, shortId } from '../lib/format';
import { isActiveOrder, PAYMENT_LABELS } from '../lib/orderHelpers';
import { queryKeys } from '../lib/queryClient';

const TABS = [
	{ key: 'all', label: 'All orders', matches: () => true },
	{ key: 'active', label: 'Active', matches: isActiveOrder },
	{ key: 'delivered', label: 'Delivered', matches: (order) => order.status === 'Delivered' },
	{ key: 'cancelled', label: 'Cancelled', matches: (order) => order.status === 'Cancelled' },
];

// My orders, also where Fawaterak's return redirect lands, which is forwarded to that order's own page
const Orders = () => {
	const { user } = useAuth();
	const [searchParams] = useSearchParams();
	const paidOrderId = searchParams.get('orderId');
	const [selectedTab, setSelectedTab] = React.useState('all');

	const { data: orders = null, error } = useQuery({
		queryKey: queryKeys.myOrders,
		queryFn: ordersApi.list,
		enabled: !paidOrderId,
	});
	const loadError = error?.message ?? '';

	if (paidOrderId) {
		const paymentResult = searchParams.get('result') ?? 'pending';
		return <Navigate to={`/orders/${paidOrderId}?result=${encodeURIComponent(paymentResult)}`} replace />;
	}

	const shownOrders = orders?.filter(TABS.find((tab) => tab.key === selectedTab).matches) ?? [];
	const totalSpent =
		orders?.filter((order) => order.status !== 'Cancelled').reduce((sum, order) => sum + order.totalAmount, 0) ?? 0;

	return (
		<div className='page flex flex-col gap-8 pb-24 pt-10'>
			<div>
				<p className='eyebrow'>My account</p>
				<h1 className='title mt-2'>My orders</h1>
				<p className='mt-2 text-[15px] text-muted'>Track your orders from payment to delivery.</p>
			</div>

			<div className='grid grid-cols-1 items-start gap-8 lg:grid-cols-3'>
				<div className='flex flex-col gap-6 lg:col-span-2'>
					<div className='flex flex-wrap gap-2'>
						{TABS.map((tab) => (
							<button
								key={tab.key}
								onClick={() => setSelectedTab(tab.key)}
								className={`chip rounded-full ${
									selectedTab === tab.key ? 'bg-brand text-white' : 'bg-surface shadow-sm hover:bg-panel'
								}`}
							>
								{tab.label} <span className='ml-1 opacity-70'>{orders?.filter(tab.matches).length ?? 0}</span>
							</button>
						))}
					</div>

					{loadError && <p className='card p-6 text-red-600'>{loadError}</p>}
					{!orders && !loadError && <p className='card p-6 text-muted'>Loading your orders...</p>}
					{orders?.length === 0 && (
						<EmptyState icon={ShoppingBag} text='You have not placed an order yet.'>
							<Link to='/' className='btn-primary'>
								Start shopping
							</Link>
						</EmptyState>
					)}
					{orders?.length > 0 && shownOrders.length === 0 && <p className='card p-6 text-muted'>No orders in this list.</p>}

					{shownOrders.map((order) => (
						<article key={order.id} className='card flex flex-col gap-5 p-6'>
							<div className='flex flex-wrap items-start justify-between gap-3'>
								<div>
									<p className='text-[18px] font-bold'>Order #{shortId(order.id)}</p>
									<p className='text-[13px] text-muted'>
										{formatDate(order.createdAt)} - {PAYMENT_LABELS[order.paymentMethod]}
									</p>
								</div>
								<StatusBadge status={order.status} />
							</div>
							{order.status !== 'Cancelled' && <OrderProgress status={order.status} />}
							<OrderItems items={order.items} />
							<div className='flex items-center justify-between gap-4'>
								<p className='text-[15px] text-muted'>
									Total <span className='ml-2 text-[18px] font-bold text-ink'>{formatPrice(order.totalAmount)}</span>
								</p>
								<Link to={`/orders/${order.id}`} className='btn-primary'>
									View order <ArrowRight size={16} />
								</Link>
							</div>
						</article>
					))}
				</div>

				<aside className='card flex flex-col gap-5 p-6'>
					<div className='flex items-center gap-4'>
						<span className='flex h-12 w-12 items-center justify-center rounded-full bg-brand text-white'>
							<User size={22} />
						</span>
						<div className='min-w-0'>
							<p className='text-[18px] font-bold'>{user.name}</p>
							<p className='truncate text-[13px] text-muted'>{user.email}</p>
						</div>
					</div>
					<div className='grid grid-cols-2 gap-3'>
						<div className='rounded bg-panel p-4'>
							<p className='field-label text-[11px] text-muted'>Orders</p>
							<p className='mt-1 text-[22px] font-bold'>{orders?.length ?? '-'}</p>
						</div>
						<div className='rounded bg-panel p-4'>
							<p className='field-label text-[11px] text-muted'>Spent</p>
							<p className='mt-1 text-[16px] font-bold'>{formatPrice(totalSpent)}</p>
						</div>
					</div>
				</aside>
			</div>
		</div>
	);
};

export default Orders;
