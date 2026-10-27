import { Link } from 'react-router-dom';
import { Users } from 'lucide-react';

import SeeAllLink from './SeeAllLink';
import StatusBadge from '../orders/StatusBadge';
import { formatDate, formatPrice, shortId } from '../../lib/format';
import { PAYMENT_LABELS } from '../../lib/orderHelpers';

// The latest orders on the overview, each opens its order page, the table scrolls sideways on small screens
// customerCount is everyone who ever placed an order
const RecentOrders = ({ orders, customerCount, seeAllTo }) => (
	<section className='card flex flex-col gap-4 overflow-hidden p-6'>
		<div className='flex items-center justify-between gap-4'>
			<h2 className='text-[18px]'>Recent orders</h2>
			<div className='flex items-center gap-4'>
				<span className='hidden items-center gap-2 text-[13px] text-muted sm:flex'>
					<Users size={16} /> {customerCount ?? '-'} customers
				</span>
				{seeAllTo && <SeeAllLink to={seeAllTo} />}
			</div>
		</div>
		<div className='-mx-6 overflow-x-auto px-6'>
			<table className='w-full min-w-[640px] text-left text-[14px]'>
				<thead>
					<tr className='field-label text-[11px] text-muted'>
						<th className='pb-3'>Order</th>
						<th className='pb-3'>Customer</th>
						<th className='pb-3'>Date</th>
						<th className='pb-3'>Payment</th>
						<th className='pb-3'>Status</th>
						<th className='pb-3 text-right'>Total</th>
					</tr>
				</thead>
				<tbody>
					{orders.map((order) => (
						<tr key={order.id} className='border-t border-line'>
							<td className='py-3 pr-4 font-bold'>
								<Link to={`/admin/orders/${order.id}`} className='hover:text-brand-dark'>
									#{shortId(order.id)}
								</Link>
							</td>
							<td className='py-3 pr-4'>{order.customerName}</td>
							<td className='py-3 pr-4 text-muted'>{formatDate(order.createdAt)}</td>
							<td className='py-3 pr-4 text-muted'>{PAYMENT_LABELS[order.paymentMethod]}</td>
							<td className='py-3 pr-4'>
								<StatusBadge status={order.status} />
							</td>
							<td className='py-3 text-right font-bold'>{formatPrice(order.totalAmount)}</td>
						</tr>
					))}
				</tbody>
			</table>
		</div>
	</section>
);

export default RecentOrders;
