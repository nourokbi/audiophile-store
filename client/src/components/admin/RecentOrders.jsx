import { Download, Users } from 'lucide-react';

import { StatusBadge } from '../OrderParts';
import { formatPrice } from '../../lib/format';

const COLUMNS = ['Order', 'Customer', 'Date', 'Payment', 'Status', 'Total'];

// The latest orders with a CSV export, the table scrolls sideways on small screens
const RecentOrders = ({ orders, customerCount, onExport }) => (
	<section className='card flex flex-col gap-4 overflow-hidden p-6'>
		<div className='flex items-center justify-between'>
			<h2 className='text-[18px]'>Recent orders</h2>
			<div className='flex items-center gap-4'>
				<span className='hidden items-center gap-2 text-[13px] text-muted sm:flex'>
					<Users size={16} /> {customerCount} customers
				</span>
				<button onClick={onExport} className='btn-secondary h-10 px-4'>
					<Download size={16} /> Export CSV
				</button>
			</div>
		</div>
		<div className='-mx-6 overflow-x-auto px-6'>
			<table className='w-full min-w-[640px] text-left text-[14px]'>
				<thead>
					<tr>
						{COLUMNS.map((column) => (
							<th
								key={column}
								className={`pb-3 text-[11px] font-bold uppercase tracking-wider text-muted ${column === 'Total' ? 'text-right' : ''}`}
							>
								{column}
							</th>
						))}
					</tr>
				</thead>
				<tbody>
					{orders.map((order) => (
						<tr key={order.id} className='border-t border-line'>
							<td className='py-3 pr-4 font-bold'>#{order.id}</td>
							<td className='py-3 pr-4'>{order.customer}</td>
							<td className='py-3 pr-4 text-muted'>{order.date}</td>
							<td className='py-3 pr-4 text-muted'>{order.method}</td>
							<td className='py-3 pr-4'>
								<StatusBadge status={order.status} />
							</td>
							<td className='py-3 text-right font-bold'>{formatPrice(order.total)}</td>
						</tr>
					))}
				</tbody>
			</table>
		</div>
	</section>
);

export default RecentOrders;
