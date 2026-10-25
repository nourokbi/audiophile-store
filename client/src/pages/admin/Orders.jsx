import React from 'react';
import { Download } from 'lucide-react';

import AdminPageHeader from '../../components/admin/AdminPageHeader';
import OrderFilters from '../../components/admin/OrderFilters';
import OrdersTable from '../../components/admin/OrdersTable';
import OrderStatusTabs from '../../components/admin/OrderStatusTabs';
import { useAdminOrders } from '../../hooks/useAdminOrders';
import { downloadCsv } from '../../lib/csv';
import { shortId } from '../../lib/format';
import { PAYMENT_LABELS } from '../../lib/orderHelpers';

// Saves the orders on screen as a CSV file that opens in Excel or Google Sheets
const exportOrders = (orders) =>
	downloadCsv(`orders-${new Date().toISOString().slice(0, 10)}.csv`, [
		['Order', 'Date', 'Customer', 'Email', 'Items', 'Payment', 'Payment status', 'Status', 'Total (EGP)'],
		...orders.map((order) => [
			shortId(order.id),
			order.createdAt.slice(0, 10),
			order.customerName,
			order.customerEmail,
			order.itemCount,
			PAYMENT_LABELS[order.paymentMethod],
			order.paymentStatus,
			order.status,
			order.totalAmount,
		]),
	]);

// Every order with its status tabs, filters and the quick step forward, the order page holds the rest of the controls
const Orders = () => {
	const orderList = useAdminOrders();
	const [savingOrderId, setSavingOrderId] = React.useState(null);
	const [saveError, setSaveError] = React.useState('');

	const moveOrderForward = async (order, nextStatus) => {
		setSavingOrderId(order.id);
		setSaveError('');
		try {
			orderList.replaceOrder(await orderList.changeOrderStatus(order.id, nextStatus));
		} catch (error) {
			setSaveError(error.message);
		} finally {
			setSavingOrderId(null);
		}
	};

	return (
		<div className='flex flex-col gap-6'>
			<AdminPageHeader
				title='Orders'
				sampleDataNote='Sample orders, a status change lasts until the page reloads. The admin orders API (Step 2) makes them real.'
			>
				<button onClick={() => exportOrders(orderList.orders)} disabled={orderList.orders.length === 0} className='btn-secondary'>
					<Download size={16} /> Export CSV
				</button>
			</AdminPageHeader>

			<OrderStatusTabs
				selectedStatus={orderList.filters.status}
				statusCounts={orderList.statusCounts}
				onSelect={(status) => orderList.setFilter('status', status)}
			/>

			<OrderFilters
				searchText={orderList.searchText}
				onSearchTextChange={orderList.setSearchText}
				filters={orderList.filters}
				onFilterChange={orderList.setFilter}
				hasFilters={orderList.hasFilters}
				onClearFilters={orderList.clearFilters}
			/>

			<OrdersTable
				orders={orderList.orders}
				totalCount={orderList.totalCount}
				isLoading={orderList.isLoading}
				hasFilters={orderList.hasFilters}
				savingOrderId={savingOrderId}
				errorMessage={saveError || orderList.loadError}
				currentPage={orderList.filters.page}
				totalPages={orderList.totalPages}
				onPageChange={(pageNumber) => orderList.setFilter('page', pageNumber)}
				onMoveForward={moveOrderForward}
			/>
		</div>
	);
};

export default Orders;
