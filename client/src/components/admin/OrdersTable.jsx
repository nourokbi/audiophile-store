import { Link } from 'react-router-dom';
import { ArrowRight } from 'lucide-react';

import StatusBadge from '../orders/StatusBadge';
import Pagination from '../ui/Pagination';
import { formatDate, formatPrice, shortId } from '../../lib/format';
import { nextStepForward, PAYMENT_LABELS, STATUS_ACTIONS } from '../../lib/orderHelpers';

const PAYMENT_STATUS_COLORS = { Pending: 'text-muted', Succeeded: 'text-emerald-600', Failed: 'text-red-600' };

// The orders and their pages, the quick button moves an order one step forward and View opens every control
const OrdersTable = ({
	orders,
	totalCount,
	isLoading,
	hasFilters,
	savingOrderId,
	errorMessage,
	currentPage,
	totalPages,
	onPageChange,
	onMoveForward,
}) => (
	<section className='card flex flex-col gap-4 overflow-hidden p-6'>
		<div className='flex flex-wrap items-center justify-between gap-4'>
			<h2 className='text-[18px]'>Orders</h2>
			<span className='text-[14px] text-muted'>
				{totalCount} {totalCount === 1 ? 'order' : 'orders'}
			</span>
		</div>
		{errorMessage && <p className='error-text'>{errorMessage}</p>}

		<div className='-mx-6 overflow-x-auto px-6'>
			<table className='w-full min-w-[900px] text-left text-[14px]'>
				<thead>
					<tr className='field-label text-[11px] text-muted'>
						<th className='pb-3'>Order</th>
						<th className='pb-3'>Customer</th>
						<th className='pb-3'>Items</th>
						<th className='pb-3'>Payment</th>
						<th className='pb-3'>Status</th>
						<th className='pb-3 text-right'>Total</th>
						<th className='pb-3 text-right'>Actions</th>
					</tr>
				</thead>
				<tbody className={isLoading ? 'opacity-60' : ''}>
					{orders.map((order) => {
						const nextStatus = nextStepForward(order);
						return (
							<tr key={order.id} className='border-t border-line'>
								<td className='py-3 pr-4'>
									<Link to={`/admin/orders/${order.id}`} className='font-bold hover:text-brand-dark'>
										#{shortId(order.id)}
									</Link>
									<p className='text-[12px] text-muted'>{formatDate(order.createdAt)}</p>
								</td>
								<td className='py-3 pr-4'>
									<p className='font-bold'>{order.customerName}</p>
									<p className='text-[12px] text-muted'>{order.customerEmail}</p>
								</td>
								<td className='py-3 pr-4 text-muted'>{order.itemCount}</td>
								<td className='py-3 pr-4'>
									<p>{PAYMENT_LABELS[order.paymentMethod]}</p>
									<p className={`text-[12px] font-bold ${PAYMENT_STATUS_COLORS[order.paymentStatus]}`}>{order.paymentStatus}</p>
								</td>
								<td className='py-3 pr-4'>
									<StatusBadge status={order.status} />
								</td>
								<td className='py-3 pr-4 text-right font-bold'>{formatPrice(order.totalAmount)}</td>
								<td className='py-3'>
									<div className='flex justify-end gap-2'>
										{nextStatus && (
											<button
												onClick={() => onMoveForward(order, nextStatus)}
												disabled={savingOrderId === order.id}
												className='btn-secondary h-9 px-3 text-[11px]'
											>
												{savingOrderId === order.id ? 'Saving...' : STATUS_ACTIONS[nextStatus]}
											</button>
										)}
										<Link to={`/admin/orders/${order.id}`} className='btn-secondary h-9 px-3 text-[11px]'>
											View <ArrowRight size={14} />
										</Link>
									</div>
								</td>
							</tr>
						);
					})}
				</tbody>
			</table>
		</div>

		{!isLoading && orders.length === 0 && (
			<p className='text-[14px] text-muted'>{hasFilters ? 'No order matches these filters.' : 'No orders yet.'}</p>
		)}
		{totalPages > 1 && <Pagination currentPage={currentPage} totalPages={totalPages} onPageChange={onPageChange} />}
	</section>
);

export default OrdersTable;
