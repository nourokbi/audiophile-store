import React from 'react';
import { Link, Navigate, useSearchParams } from 'react-router-dom';

import { api } from '../lib/api';
import { formatPrice, shortId } from '../lib/format';

// "My orders". Also the landing spot of Fawaterak's return redirect, which arrives as
// /orders?orderId=…&result=…: that is forwarded to the order's own progress page.
const Orders = () => {
	const [params] = useSearchParams();
	const fromPayment = params.get('orderId');
	const [orders, setOrders] = React.useState(null);
	const [error, setError] = React.useState('');

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

	return (
		<main className='mt-[95px] bg-gray-light'>
			<div className='xs:px-[24px] md:px-[40px] lg:px-[165px] xs:pt-10 md:pt-16 lg:pt-[79px] pb-[140px]'>
				<section className='bg-white xs:p-6 md:p-12 rounded-[8px] w-full'>
					<h3>My orders</h3>
					{error && <p className='p text-red-600 mt-6'>{error}</p>}
					{!orders && !error && <p className='p text-black/50 mt-6'>Loading…</p>}
					{orders?.length === 0 && (
						<div className='mt-6'>
							<p className='p text-black/50'>You haven’t placed an order yet.</p>
							<Link to='/'>
								<button className='btn bg-cream hover:bg-cream-light mt-6'>Start shopping</button>
							</Link>
						</div>
					)}
					{orders?.length > 0 && (
						<ul className='mt-8 flex flex-col gap-3'>
							{orders.map((o) => (
								<li key={o.id}>
									<Link
										to={`/orders/${o.id}`}
										className='flex xs:flex-col md:flex-row md:items-center justify-between gap-2 rounded-lg border border-stone-300 hover:border-cream transition-all duration-150 px-6 py-4'
									>
										<div>
											<p className='text-[15px] font-bold text-black'>Order #{shortId(o.id)}</p>
											<p className='text-[14px] text-black/50'>
												{new Date(o.createdAt).toLocaleString()} · {o.items.length} item
												{o.items.length === 1 ? '' : 's'}
											</p>
										</div>
										<div className='flex items-center gap-4'>
											<StatusPill status={o.status} />
											<p className='text-[15px] font-bold text-black'>{formatPrice(o.totalAmount)}</p>
										</div>
									</Link>
								</li>
							))}
						</ul>
					)}
				</section>
			</div>
		</main>
	);
};

const PILL = {
	Pending: 'bg-gray-light text-black/60',
	Confirmed: 'bg-cream text-white',
	Processing: 'bg-cream text-white',
	Shipped: 'bg-stone text-white',
	Delivered: 'bg-stone text-white',
	Cancelled: 'bg-red-100 text-red-700',
};

export const StatusPill = ({ status }) => (
	<span className={`rounded-full px-3 py-1 text-[12px] font-bold uppercase tracking-[1px] ${PILL[status] ?? ''}`}>
		{status}
	</span>
);

export default Orders;
