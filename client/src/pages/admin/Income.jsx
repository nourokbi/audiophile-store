import { useSearchParams } from 'react-router-dom';
import { Receipt, ShoppingBag, Wallet, XCircle } from 'lucide-react';

import AdminPageHeader from '../../components/admin/AdminPageHeader';
import StatCard from '../../components/admin/StatCard';
import BarChart from '../../components/charts/BarChart';
import ChartCard from '../../components/charts/ChartCard';
import LineChart from '../../components/charts/LineChart';
import { useOrderStats } from '../../hooks/useOrderStats';
import { formatPrice } from '../../lib/format';
import { ORDER_STATUSES, PAYMENT_LABELS } from '../../lib/orderHelpers';
import { dayLabel, revenueChangeNote, sumOf } from '../../lib/statsHelpers';

const PERIODS = [7, 30, 90, 365];
const DEFAULT_PERIOD = 30;

// The business over a chosen period: the headline numbers, revenue and orders per day, and where the money comes from
// The period lives in the address bar so a refresh or a shared link keeps it
const Income = () => {
	const [searchParams, setSearchParams] = useSearchParams();
	const days = PERIODS.includes(Number(searchParams.get('days'))) ? Number(searchParams.get('days')) : DEFAULT_PERIOD;
	const { stats, loadError } = useOrderStats(days);

	return (
		<div className='flex flex-col gap-6'>
			<AdminPageHeader title='Income'>
				<div role='group' aria-label='Period' className='flex rounded bg-panel p-1'>
					{PERIODS.map((period) => (
						<button
							key={period}
							onClick={() => setSearchParams({ days: String(period) })}
							aria-pressed={period === days}
							className={`rounded px-3 py-1.5 text-[13px] font-bold ${
								period === days ? 'bg-surface text-ink shadow-sm' : 'text-muted hover:text-ink'
							}`}
						>
							{period === 365 ? '1 year' : `${period} days`}
						</button>
					))}
				</div>
			</AdminPageHeader>
			{loadError && <p className='error-text'>{loadError}</p>}

			{!stats ? <p className='text-muted'>Loading the numbers...</p> : <IncomeCharts stats={stats} />}
		</div>
	);
};

// Every chart is one series in the brand colour, so a bar's length or a line's height is the only thing to read
const IncomeCharts = ({ stats }) => {
	const dayLabels = stats.perDay.map((entry) => dayLabel(entry.day));
	const averageOrder = stats.paidOrderCount > 0 ? stats.revenue / stats.paidOrderCount : 0;
	const categories = [...stats.byCategory].sort((a, b) => b.revenue - a.revenue);

	return (
		<>
			<div className='grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4'>
				<StatCard icon={Wallet} label='Revenue' value={formatPrice(stats.revenue)} note={revenueChangeNote(stats)} />
				<StatCard
					icon={ShoppingBag}
					label='Paid orders'
					value={stats.paidOrderCount}
					note={`${stats.orderCount} placed in ${stats.days} days`}
				/>
				<StatCard
					icon={Receipt}
					label='Average order'
					value={stats.paidOrderCount > 0 ? formatPrice(averageOrder) : '-'}
					note='Revenue per paid order'
				/>
				<StatCard
					icon={XCircle}
					label='Cancelled'
					value={stats.statusCounts.Cancelled}
					note={`Of ${stats.orderCount} orders placed`}
				/>
			</div>

			<ChartCard
				title='Revenue per day'
				summary={formatPrice(stats.revenue)}
				rows={stats.perDay.map((entry) => ({ label: dayLabel(entry.day), value: formatPrice(entry.revenue) }))}
				height='h-72'
			>
				<LineChart
					labels={dayLabels}
					values={stats.perDay.map((entry) => entry.revenue)}
					formatValue={formatPrice}
					label={`Revenue per day for the last ${stats.days} days`}
				/>
			</ChartCard>

			<ChartCard
				title='Orders per day'
				summary={`${sumOf(stats.perDay, 'orders')} orders, cancelled included`}
				rows={stats.perDay.map((entry) => ({ label: dayLabel(entry.day), value: entry.orders }))}
			>
				<BarChart
					labels={dayLabels}
					values={stats.perDay.map((entry) => entry.orders)}
					formatValue={(orders) => `${orders} orders`}
					label={`Orders per day for the last ${stats.days} days`}
				/>
			</ChartCard>

			<div className='grid grid-cols-1 gap-6 xl:grid-cols-2'>
				<ChartCard
					title='Revenue by category'
					summary='Items only, shipping left out'
					rows={categories.map((entry) => ({ label: entry.category, value: formatPrice(entry.revenue) }))}
				>
					<BarChart
						labels={categories.map((entry) => entry.category)}
						values={categories.map((entry) => entry.revenue)}
						formatValue={formatPrice}
						isHorizontal
						label='Revenue by product category'
					/>
				</ChartCard>

				<ChartCard
					title='Top products'
					summary={`The best ${stats.topProducts.length || ''} by revenue`}
					rows={stats.topProducts.map((entry) => ({
						label: `${entry.productName} (${entry.unitsSold} sold)`,
						value: formatPrice(entry.revenue),
					}))}
				>
					{stats.topProducts.length === 0 ? (
						<p className='flex h-full items-center justify-center text-[14px] text-muted'>No paid orders in this period.</p>
					) : (
						<BarChart
							labels={stats.topProducts.map((entry) => entry.productName)}
							values={stats.topProducts.map((entry) => entry.revenue)}
							formatValue={formatPrice}
							isHorizontal
							label='The products that brought the most revenue'
						/>
					)}
				</ChartCard>

				<ChartCard
					title='Revenue by payment method'
					rows={stats.byPaymentMethod.map((entry) => ({
						label: `${PAYMENT_LABELS[entry.paymentMethod]} (${entry.orders} orders)`,
						value: formatPrice(entry.revenue),
					}))}
					height='h-40'
				>
					<BarChart
						labels={stats.byPaymentMethod.map((entry) => PAYMENT_LABELS[entry.paymentMethod])}
						values={stats.byPaymentMethod.map((entry) => entry.revenue)}
						formatValue={formatPrice}
						isHorizontal
						label='Revenue by payment method'
					/>
				</ChartCard>

				<ChartCard
					title='Orders by status'
					summary='Orders placed in the period, where they are now'
					rows={ORDER_STATUSES.map((status) => ({ label: status, value: stats.statusCounts[status] }))}
				>
					<BarChart
						labels={ORDER_STATUSES}
						values={ORDER_STATUSES.map((status) => stats.statusCounts[status])}
						formatValue={(orders) => `${orders} orders`}
						isHorizontal
						label='Orders placed in the period by their status now'
					/>
				</ChartCard>
			</div>
		</>
	);
};

export default Income;
