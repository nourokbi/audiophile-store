import { ORDER_STATUSES } from '../../lib/orderHelpers';

// All orders and one tab per status, each with how many orders it holds for the other filters
const OrderStatusTabs = ({ selectedStatus, statusCounts, onSelect }) => {
	const allCount = Object.values(statusCounts).reduce((sum, count) => sum + count, 0);
	const tabs = [
		{ status: '', label: 'All', count: allCount },
		...ORDER_STATUSES.map((status) => ({ status, label: status, count: statusCounts[status] ?? 0 })),
	];

	return (
		<div role='tablist' aria-label='Order status' className='flex flex-wrap gap-2'>
			{tabs.map(({ status, label, count }) => (
				<button
					key={label}
					role='tab'
					aria-selected={selectedStatus === status}
					onClick={() => onSelect(status)}
					className={`chip rounded-full ${
						selectedStatus === status ? 'bg-brand text-white' : 'bg-surface shadow-sm hover:bg-panel'
					}`}
				>
					{label} <span className='ml-1 opacity-70'>{count}</span>
				</button>
			))}
		</div>
	);
};

export default OrderStatusTabs;
