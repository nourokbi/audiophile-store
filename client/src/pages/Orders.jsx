import React from 'react';
import { Link, Navigate, useSearchParams } from 'react-router-dom';
import { ArrowRight, ShoppingBag, User } from 'lucide-react';

import { OrderItems, OrderProgress, StatusBadge } from '../components/OrderParts';
import { useAuth } from '../context/AuthContext';
import { api } from '../lib/api';
import { formatPrice, shortId } from '../lib/format';

const TABS = [
	{ key: 'all', label: 'All orders', matches: () => true },
	{ key: 'active', label: 'Active', matches: (o) => !['Delivered', 'Cancelled'].includes(o.status) },
	{ key: 'delivered', label: 'Delivered', matches: (o) => o.status === 'Delivered' },
	{ key: 'cancelled', label: 'Cancelled', matches: (o) => o.status === 'Cancelled' },
];

// My orders, also where Fawaterak's return redirect lands, which is forwarded to that order's own page
const Orders = () => {
	const { user } = useAuth();
	const [params] = useSearchParams();
	const fromPayment = params.get('orderId');
	const [orders, setOrders] = React.useState(null);
	const [error, setError] = React.useState('');
	const [tab, setTab] = React.useState('all');

	React.useEffect(() => {
		if (fromPayment) return;
		api('/orders')
			.then(setOrders)
			.catch((e) => setError(e.message));
	}, [fromPayment]);

	if (fromPayment) {
		const result = params.get('result') ?? 'pending';
		return <Navigate to={`/orders/${fromPayment}?result=${encodeURIComponent(result)}`} replace />;
	}

	const shown = orders?.filter(TABS.find((t) => t.key === tab).matches) ?? [];
	const spent = orders?.filter((o) => o.status !== 'Cancelled').reduce((sum, o) => sum + o.totalAmount, 0) ?? 0;

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
						{TABS.map((t) => {
							const count = orders?.filter(t.matches).length ?? 0;
							return (
								<button
									key={t.key}
									onClick={() => setTab(t.key)}
									className={`chip rounded-full ${tab === t.key ? 'bg-brand text-white' : 'bg-white shadow-sm hover:bg-panel'}`}
								>
									{t.label} <span className='ml-1 opacity-70'>{count}</span>
								</button>
							);
						})}
					</div>

					{error && <p className='card p-6 text-red-600'>{error}</p>}
					{!orders && !error && <p className='card p-6 text-muted'>Loading your orders...</p>}
					{orders?.length === 0 && (
						<div className='card flex flex-col items-center gap-4 p-10 text-center'>
							<ShoppingBag size={32} className='text-muted' />
							<p className='text-muted'>You have not placed an order yet.</p>
							<Link to='/' className='btn-primary'>
								Start shopping
							</Link>
						</div>
					)}
					{orders?.length > 0 && shown.length === 0 && (
						<p className='card p-6 text-muted'>No orders in this list.</p>
					)}

					{shown.map((order) => (
						<article key={order.id} className='card flex flex-col gap-5 p-6'>
							<div className='flex flex-wrap items-start justify-between gap-3'>
								<div>
									<p className='text-[18px] font-bold'>Order #{shortId(order.id)}</p>
									<p className='text-[13px] text-muted'>
										{new Date(order.createdAt).toLocaleString()} -{' '}
										{order.paymentMethod === 'Card' ? 'Card' : 'Cash on delivery'}
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
							<p className='text-[11px] font-bold uppercase tracking-wider text-muted'>Orders</p>
							<p className='mt-1 text-[22px] font-bold'>{orders?.length ?? '-'}</p>
						</div>
						<div className='rounded bg-panel p-4'>
							<p className='text-[11px] font-bold uppercase tracking-wider text-muted'>Spent</p>
							<p className='mt-1 text-[16px] font-bold'>{formatPrice(spent)}</p>
						</div>
					</div>
				</aside>
			</div>
		</div>
	);
};

export default Orders;
