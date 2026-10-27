import AdminPageHeader from '../../components/admin/AdminPageHeader';
import LowStockList from '../../components/admin/LowStockList';
import RecentOrders from '../../components/admin/RecentOrders';
import StatCards from '../../components/admin/StatCards';
import BarChart from '../../components/charts/BarChart';
import ChartCard from '../../components/charts/ChartCard';
import { useInventorySummary } from '../../hooks/useInventorySummary';
import { useOrderStats } from '../../hooks/useOrderStats';
import { useRecentOrders } from '../../hooks/useRecentOrders';
import { LOW_STOCK_LIMIT } from '../../lib/productHelpers';
import { dayLabel, sumOf } from '../../lib/statsHelpers';

const PREVIEW_COUNT = 5;
const STATS_DAYS = 30;
const LOW_STOCK_PAGE = '/admin/products?availability=OnSale&lowStockOnly=true&sort=StockLowToHigh';

// The business at a glance: the four numbers and a short preview of each admin page, the income page has every chart
const Overview = () => {
	const inventory = useInventorySummary(PREVIEW_COUNT);
	const orderStats = useOrderStats(STATS_DAYS);
	const recentOrders = useRecentOrders(PREVIEW_COUNT);
	const loadError = inventory.loadError || orderStats.loadError;
	const perDay = orderStats.stats?.perDay ?? [];

	return (
		<div className='flex flex-col gap-6'>
			<AdminPageHeader title='Overview' />
			{loadError && <p className='error-text'>{loadError}</p>}

			<StatCards
				orderStats={orderStats.stats}
				productCount={inventory.onSaleCount}
				lowStockCount={inventory.lowStockCount}
				lowStockLimit={LOW_STOCK_LIMIT}
			/>

			<div className='grid grid-cols-1 items-start gap-6 xl:grid-cols-3'>
				<ChartCard
					title={`Orders per day, last ${STATS_DAYS} days`}
					summary={`${sumOf(perDay, 'orders')} orders`}
					rows={perDay.map((entry) => ({ label: dayLabel(entry.day), value: entry.orders }))}
					className='xl:col-span-2'
				>
					<BarChart
						labels={perDay.map((entry) => dayLabel(entry.day))}
						values={perDay.map((entry) => entry.orders)}
						formatValue={(orders) => `${orders} orders`}
						label={`Orders per day for the last ${STATS_DAYS} days`}
					/>
				</ChartCard>
				<LowStockList products={inventory.lowStockProducts} lowStockLimit={LOW_STOCK_LIMIT} seeAllTo={LOW_STOCK_PAGE} />
			</div>

			<RecentOrders orders={recentOrders} customerCount={orderStats.stats?.customerCount} seeAllTo='/admin/orders' />
		</div>
	);
};

export default Overview;
