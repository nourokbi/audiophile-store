import { TrendingUp } from 'lucide-react';

// Orders per day as plain bars, the busiest day is full height and orange, Chart.js replaces it with GET /admin/stats
const OrdersChart = ({ ordersByDay, changePercent }) => {
	const busiestDayOrders = Math.max(...ordersByDay.map((entry) => entry.orders));

	return (
		<section className='card flex flex-col gap-6 p-6 lg:col-span-2'>
			<div className='flex items-center justify-between'>
				<h2 className='text-[18px]'>Orders this week</h2>
				<span className='flex items-center gap-1 text-[13px] font-bold text-emerald-600'>
					<TrendingUp size={16} /> {changePercent}%
				</span>
			</div>
			<div className='flex h-48 items-end gap-3'>
				{ordersByDay.map(({ day, orders }) => (
					<div key={day} className='flex h-full flex-1 flex-col items-center justify-end gap-2'>
						<span className='text-[12px] font-bold'>{orders}</span>
						<div
							className={`w-full rounded-t ${orders === busiestDayOrders ? 'bg-brand' : 'bg-brand/40'}`}
							style={{ height: `${(orders / busiestDayOrders) * 100}%` }}
						/>
						<span className='text-[12px] text-muted'>{day}</span>
					</div>
				))}
			</div>
		</section>
	);
};

export default OrdersChart;
