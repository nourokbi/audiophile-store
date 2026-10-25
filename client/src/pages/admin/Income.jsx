import AdminPageHeader from '../../components/admin/AdminPageHeader';
import OrdersChart from '../../components/admin/OrdersChart';
import { SAMPLE_SALES_BY_DAY, SAMPLE_STATS } from '../../data/adminSample';

// Revenue over time, per payment method and per category once GET /admin/stats/income exists, until then the sample chart
const Income = () => (
	<div className='flex flex-col gap-6'>
		<AdminPageHeader
			title='Income'
			sampleDataNote='Sample numbers. Revenue per day, per payment method and per category with Chart.js come with the income API.'
		/>
		<OrdersChart ordersByDay={SAMPLE_SALES_BY_DAY} changePercent={SAMPLE_STATS.revenueChange} />
	</div>
);

export default Income;
