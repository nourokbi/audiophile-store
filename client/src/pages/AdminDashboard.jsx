import React from 'react';
import { Info, Plus } from 'lucide-react';

import LowStockList from '../components/admin/LowStockList';
import NumberDialog from '../components/admin/NumberDialog';
import OrdersChart from '../components/admin/OrdersChart';
import ProductDialog from '../components/admin/ProductDialog';
import ProductsTable from '../components/admin/ProductsTable';
import RecentOrders from '../components/admin/RecentOrders';
import StatCards from '../components/admin/StatCards';
import { SAMPLE_ORDERS, SAMPLE_SALES_BY_DAY, SAMPLE_STATS } from '../data/adminSample';
import { useAdminProducts } from '../hooks/useAdminProducts';
import { adminApi } from '../lib/adminApi';
import { downloadCsv } from '../lib/csv';
import { formatPrice } from '../lib/format';

const LOW_STOCK_LIMIT = 8;
const NEW_PRODUCT_ID = 'new'; // marks the add product form as saving, it has no id yet

// Saves the recent orders as a CSV file that opens in Excel or Google Sheets
const exportRecentOrders = () =>
	downloadCsv(`recent-orders-${new Date().toISOString().slice(0, 10)}.csv`, [
		['Order', 'Customer', 'Date', 'Payment', 'Status', 'Total (EGP)'],
		...SAMPLE_ORDERS.map((order) => [order.id, order.customer, order.date, order.method, order.status, order.total]),
	]);

// The admin overview and the one place with its logic, the components under components/admin only draw what they are given
// Products come from the API, the stats, chart and orders are sample data until Steps 2 and 3
const AdminDashboard = () => {
	const productList = useAdminProducts();
	const [openDialog, setOpenDialog] = React.useState(null); // { type: 'add' | 'restock' | 'price', product }
	const [savingProductId, setSavingProductId] = React.useState(null);
	const [saveError, setSaveError] = React.useState('');

	const showDialog = (type, product) => {
		setSaveError('');
		setOpenDialog({ type, product });
	};
	const closeDialog = () => setOpenDialog(null);

	// Every change goes through here: send the request, then hand the product the server answered to applyResult
	const saveChange = async (productId, sendRequest, applyResult) => {
		setSavingProductId(productId);
		setSaveError('');
		try {
			applyResult(await sendRequest());
			setOpenDialog(null);
		} catch (error) {
			setSaveError(error.message);
		} finally {
			setSavingProductId(null);
		}
	};

	const toggleProductAvailability = (product) =>
		saveChange(product.id, () => adminApi.setAvailability(product.id, !product.isActive), productList.replaceProduct);
	const restockProduct = (product, quantity) =>
		saveChange(product.id, () => adminApi.restock(product.id, quantity), productList.replaceProduct);
	const changeProductPrice = (product, price) =>
		saveChange(product.id, () => adminApi.changePrice(product.id, price), productList.replaceProduct);
	const createProduct = (productRequest) =>
		saveChange(NEW_PRODUCT_ID, () => adminApi.createProduct(productRequest), productList.reloadFromFirstPage);

	const lowStockProducts = productList.products
		.filter((product) => product.stock <= LOW_STOCK_LIMIT)
		.sort((a, b) => a.stock - b.stock);
	const unitsInStock = productList.products.reduce((sum, product) => sum + product.stock, 0);
	const dialogProduct = openDialog?.product;

	return (
		<div className='page flex flex-col gap-8 pb-24 pt-10'>
			<div className='flex flex-wrap items-end justify-between gap-4'>
				<div>
					<p className='eyebrow'>Admin</p>
					<h1 className='title mt-2'>Dashboard</h1>
				</div>
				<button onClick={() => showDialog('add')} className='btn-primary'>
					<Plus size={16} /> Add product
				</button>
			</div>

			<p className='flex items-center gap-3 rounded bg-brand-soft p-4 text-[14px] text-brand-dark'>
				<Info size={18} className='shrink-0' />
				Revenue, the sales chart and recent orders are sample data until the orders and stats API is ready. Products and stock are
				live and every change is saved.
			</p>

			<StatCards
				stats={SAMPLE_STATS}
				unitsInStock={unitsInStock}
				productCount={productList.products.length}
				lowStockCount={lowStockProducts.length}
				lowStockLimit={LOW_STOCK_LIMIT}
			/>

			<div className='grid grid-cols-1 items-start gap-6 lg:grid-cols-3'>
				<OrdersChart ordersByDay={SAMPLE_SALES_BY_DAY} changePercent={SAMPLE_STATS.revenueChange} />
				<LowStockList products={lowStockProducts} lowStockLimit={LOW_STOCK_LIMIT} />
			</div>

			<RecentOrders orders={SAMPLE_ORDERS} customerCount={SAMPLE_STATS.customers} onExport={exportRecentOrders} />

			<ProductsTable
				products={productList.products}
				lowStockLimit={LOW_STOCK_LIMIT}
				savingProductId={savingProductId}
				errorMessage={openDialog ? '' : saveError || productList.loadError}
				searchText={productList.searchText}
				onSearchTextChange={productList.setSearchText}
				currentPage={productList.currentPage}
				totalPages={productList.totalPages}
				onPageChange={productList.setCurrentPage}
				onToggleAvailability={toggleProductAvailability}
				onRestockClick={(product) => showDialog('restock', product)}
				onPriceClick={(product) => showDialog('price', product)}
			/>

			{openDialog?.type === 'add' && (
				<ProductDialog
					isSaving={savingProductId === NEW_PRODUCT_ID}
					errorMessage={saveError}
					onClose={closeDialog}
					onSave={createProduct}
				/>
			)}

			{openDialog?.type === 'restock' && (
				<NumberDialog
					title={`Restock ${dialogProduct.name}`}
					label='Units to add'
					initialValue={10}
					minimum={1}
					isSaving={savingProductId === dialogProduct.id}
					errorMessage={saveError}
					describeChange={(quantity) => `Stock goes from ${dialogProduct.stock} to ${dialogProduct.stock + quantity}`}
					onClose={closeDialog}
					onSave={(quantity) => restockProduct(dialogProduct, quantity)}
				/>
			)}

			{openDialog?.type === 'price' && (
				<NumberDialog
					title={`Price of ${dialogProduct.name}`}
					label='New price in EGP'
					initialValue={dialogProduct.price}
					minimum={1}
					isSaving={savingProductId === dialogProduct.id}
					errorMessage={saveError}
					describeChange={(price) => `Was ${formatPrice(dialogProduct.price)}, becomes ${formatPrice(price)}`}
					onClose={closeDialog}
					onSave={(price) => changeProductPrice(dialogProduct, price)}
				/>
			)}
		</div>
	);
};

export default AdminDashboard;
