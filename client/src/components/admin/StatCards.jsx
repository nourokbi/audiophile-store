import { AlertTriangle, Boxes, Package, Wallet } from 'lucide-react';

import { formatPrice } from '../../lib/format';

// The four numbers across the top of the overview, each one counts the whole business and not the page in view
const StatCards = ({ stats, productCount, lowStockCount, lowStockLimit }) => (
	<div className='grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4'>
		<StatCard icon={Wallet} label='Revenue, 30 days' value={formatPrice(stats.revenue)} note={`+${stats.revenueChange}%`} />
		<StatCard icon={Package} label='Active orders' value={stats.activeOrders} note={`${stats.ordersThisWeek} this week`} />
		<StatCard icon={Boxes} label='Products on sale' value={productCount ?? '-'} note='Shown in the shop' />
		<StatCard
			icon={AlertTriangle}
			label='Low stock'
			value={lowStockCount ?? '-'}
			note={`${lowStockLimit} or fewer left`}
			isWarning={lowStockCount > 0}
		/>
	</div>
);

const StatCard = ({ icon: Icon, label, value, note, isWarning = false }) => (
	<div className={`card flex flex-col gap-3 p-5 ${isWarning ? 'border-l-4 border-brand' : ''}`}>
		<div className='flex items-center justify-between'>
			<span className='field-label text-muted'>{label}</span>
			<span className='flex h-9 w-9 items-center justify-center rounded bg-panel text-brand'>
				<Icon size={18} />
			</span>
		</div>
		<span className={`text-[28px] font-bold leading-none ${isWarning ? 'text-brand' : ''}`}>{value}</span>
		<span className='text-[13px] text-muted'>{note}</span>
	</div>
);

export default StatCards;
