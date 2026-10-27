import AdminPageHeader from '../../components/admin/AdminPageHeader';
import LowStockList from '../../components/admin/LowStockList';
import OrdersChart from '../../components/admin/OrdersChart';
import RecentOrders from '../../components/admin/RecentOrders';
import StatCards from '../../components/admin/StatCards';
import { SAMPLE_SALES_BY_DAY, SAMPLE_STATS } from '../../data/adminSample';
import { useInventorySummary } from '../../hooks/useInventorySummary';
import { useRecentOrders } from '../../hooks/useRecentOrders';
import { LOW_STOCK_LIMIT } from '../../lib/productHelpers';

const PREVIEW_COUNT = 5;
const LOW_STOCK_PAGE = '/admin/products?availability=OnSale&lowStockOnly=true&sort=StockLowToHigh';

// The business at a glance: the four numbers and a short preview of each admin page
const Overview = () => {
	const inventory = useInventorySummary(PREVIEW_COUNT);
	const recentOrders = useRecentOrders(PREVIEW_COUNT);

	return (
		<div className='flex flex-col gap-6'>
			<AdminPageHeader
				title='Overview'
				sampleDataNote='Revenue, active orders and the chart are sample numbers until the stats API (Step 3). Products and recent orders are live.'
			/>
			{inventory.loadError && <p className='error-text'>{inventory.loadError}</p>}

			<StatCards
				stats={SAMPLE_STATS}
				productCount={inventory.onSaleCount}
				lowStockCount={inventory.lowStockCount}
				lowStockLimit={LOW_STOCK_LIMIT}
			/>

			<div className='grid grid-cols-1 items-start gap-6 xl:grid-cols-3'>
				<OrdersChart ordersByDay={SAMPLE_SALES_BY_DAY} changePercent={SAMPLE_STATS.revenueChange} />
				<LowStockList products={inventory.lowStockProducts} lowStockLimit={LOW_STOCK_LIMIT} seeAllTo={LOW_STOCK_PAGE} />
			</div>

			<RecentOrders orders={recentOrders} customerCount={SAMPLE_STATS.customers} seeAllTo='/admin/orders' />
		</div>
	);
};

export default Overview;
