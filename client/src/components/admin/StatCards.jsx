import { AlertTriangle, Boxes, Package, Wallet } from 'lucide-react';

import StatCard from './StatCard';
import { formatPrice } from '../../lib/format';
import { revenueChangeNote } from '../../lib/statsHelpers';

// The four numbers across the top of the overview, each one counts the whole business and not the page in view
// orderStats is null until GET /admin/orders/stats answers, the order cards show a dash until then
const StatCards = ({ orderStats, productCount, lowStockCount, lowStockLimit }) => (
	<div className='grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4'>
		<StatCard
			icon={Wallet}
			label={`Revenue, ${orderStats?.days ?? 30} days`}
			value={orderStats ? formatPrice(orderStats.revenue) : '-'}
			note={orderStats ? revenueChangeNote(orderStats) : 'Paid orders that were not cancelled'}
		/>
		<StatCard
			icon={Package}
			label='Active orders'
			value={orderStats?.activeOrders ?? '-'}
			note={orderStats ? `${orderStats.orderCount} placed in ${orderStats.days} days` : 'Not delivered or cancelled yet'}
		/>
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

export default StatCards;
