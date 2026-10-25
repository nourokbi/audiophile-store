import { useEffect, useState } from 'react';
import { AlertTriangle, Boxes, Download, Info, Package, Plus, TrendingUp, Users, Wallet } from 'lucide-react';

import AdminProducts from '../components/AdminProducts';
import ProductDialog from '../components/ProductDialog';
import { StatusBadge } from '../components/OrderParts';
import { useCatalog } from '../context/CatalogContext';
import { SAMPLE_ORDERS, SAMPLE_SALES_BY_DAY, SAMPLE_STATS } from '../data/adminSample';
import { adminApi } from '../lib/adminApi';
import { downloadCsv } from '../lib/csv';
import { formatPrice } from '../lib/format';

const LOW_STOCK = 8;

// Saves the recent orders as a CSV file that opens in Excel or Google Sheets
const exportRecentOrders = () =>
	downloadCsv(`recent-orders-${new Date().toISOString().slice(0, 10)}.csv`, [
		['Order', 'Customer', 'Date', 'Payment', 'Status', 'Total (EGP)'],
		...SAMPLE_ORDERS.map((o) => [o.id, o.customer, o.date, o.method, o.status, o.total]),
	]);

// The admin overview, products come from the API and the stats, chart and orders are sample data until Steps 2 and 3
const AdminDashboard = () => {
	const [products, setProducts] = useState([]);
	const [page, setPage] = useState(1);
	const [totalPages, setTotalPages] = useState(1);
	const [search, setSearch] = useState(''); // what is typed in the box
	const [query, setQuery] = useState(''); // what is searched, set 300 ms after the last key
	const [reloadKey, setReloadKey] = useState(0); // bumped to load the table again, after a product is added
	const [showAdd, setShowAdd] = useState(false);

	// Waits until typing stops so the API gets one request instead of one per letter, a new search starts on page 1
	useEffect(() => {
		const timer = setTimeout(() => {
			setQuery(search.trim());
			setPage(1);
		}, 300);
		return () => clearTimeout(timer);
	}, [search]);

	// Loads the page from the list or the search, an answer that arrives after a newer request is ignored
	useEffect(() => {
		let stale = false;
		const load = query ? adminApi.searchProducts(query, page) : adminApi.listProducts(page);
		load
			.then((result) => {
				if (stale) return;
				setProducts(result.items);
				setTotalPages(result.totalPages);
			})
			.catch(console.error);
		return () => {
			stale = true;
		};
	}, [page, query, reloadKey]);

	const { reload: reloadShop } = useCatalog();

	// Swaps in the product the server answered and reloads the shop's list so a shown or hidden product appears there at once
	const onChange = (updated) => {
		setProducts((all) => all.map((p) => (p.id === updated.id ? updated : p)));
		reloadShop();
	};

	// A new product can land on any page, so the table goes back to the full list on page 1
	const onCreated = () => {
		setShowAdd(false);
		setSearch('');
		setPage(1);
		setReloadKey((k) => k + 1);
		reloadShop();
	};
	const lowStock = products.filter((p) => p.stock <= LOW_STOCK).sort((a, b) => a.stock - b.stock);
	const unitsInStock = products.reduce((sum, p) => sum + p.stock, 0);
	const busiestDay = Math.max(...SAMPLE_SALES_BY_DAY.map((d) => d.orders));

	return (
		<div className='page flex flex-col gap-8 pb-24 pt-10'>
			<div className='flex flex-wrap items-end justify-between gap-4'>
				<div>
					<p className='eyebrow'>Admin</p>
					<h1 className='title mt-2'>Dashboard</h1>
				</div>
				<button onClick={() => setShowAdd(true)} className='btn-primary'>
					<Plus size={16} /> Add product
				</button>
			</div>

			<p className='flex items-center gap-3 rounded bg-brand-soft p-4 text-[14px] text-brand-dark'>
				<Info size={18} className='shrink-0' />
				Revenue, the sales chart and recent orders are sample data until the orders and stats API is ready. Products and stock are
				live and every change is saved.
			</p>

			<div className='grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4'>
				<Stat
					icon={Wallet}
					label='Revenue, 30 days'
					value={formatPrice(SAMPLE_STATS.revenue)}
					note={`+${SAMPLE_STATS.revenueChange}%`}
				/>
				<Stat
					icon={Package}
					label='Active orders'
					value={SAMPLE_STATS.activeOrders}
					note={`${SAMPLE_STATS.ordersThisWeek} this week`}
				/>
				<Stat icon={Boxes} label='Units in stock' value={unitsInStock} note={`${products.length} products`} />
				<Stat
					icon={AlertTriangle}
					label='Low stock'
					value={lowStock.length}
					note={`${LOW_STOCK} or fewer left`}
					warn={lowStock.length > 0}
				/>
			</div>

			<div className='grid grid-cols-1 items-start gap-6 lg:grid-cols-3'>
				<section className='card flex flex-col gap-6 p-6 lg:col-span-2'>
					<div className='flex items-center justify-between'>
						<h2 className='text-[18px]'>Orders this week</h2>
						<span className='flex items-center gap-1 text-[13px] font-bold text-emerald-600'>
							<TrendingUp size={16} /> {SAMPLE_STATS.revenueChange}%
						</span>
					</div>
					<div className='flex h-48 items-end gap-3'>
						{SAMPLE_SALES_BY_DAY.map(({ day, orders }) => (
							<div key={day} className='flex h-full flex-1 flex-col items-center justify-end gap-2'>
								<span className='text-[12px] font-bold'>{orders}</span>
								<div
									className={`w-full rounded-t ${orders === busiestDay ? 'bg-brand' : 'bg-brand/40'}`}
									style={{ height: `${(orders / busiestDay) * 100}%` }}
								/>
								<span className='text-[12px] text-muted'>{day}</span>
							</div>
						))}
					</div>
				</section>

				<section className='card flex flex-col gap-4 p-6'>
					<h2 className='flex items-center gap-2 text-[18px]'>
						<AlertTriangle size={18} className='text-brand' /> Low stock
					</h2>
					{lowStock.length === 0 && <p className='text-[14px] text-muted'>Every product has more than {LOW_STOCK} left.</p>}
					<ul className='flex flex-col gap-3'>
						{lowStock.map((p) => (
							<li key={p.slug} className='flex items-center justify-between gap-3 rounded bg-panel p-3'>
								<div className='flex min-w-0 items-center gap-3'>
									<img src={p.image.mobile} alt='' className='h-10 w-10 rounded object-cover' />
									<span className='truncate text-[14px] font-bold'>{p.name}</span>
								</div>
								<span className='shrink-0 text-[13px] font-bold text-brand-dark'>{p.stock} left</span>
							</li>
						))}
					</ul>
				</section>
			</div>

			<section className='card flex flex-col gap-4 overflow-hidden p-6'>
				<div className='flex items-center justify-between'>
					<h2 className='text-[18px]'>Recent orders</h2>
					<div className='flex items-center gap-4'>
						<span className='hidden items-center gap-2 text-[13px] text-muted sm:flex'>
							<Users size={16} /> {SAMPLE_STATS.customers} customers
						</span>
						<button onClick={exportRecentOrders} className='btn-secondary h-10 px-4'>
							<Download size={16} /> Export CSV
						</button>
					</div>
				</div>
				<Table head={['Order', 'Customer', 'Date', 'Payment', 'Status', 'Total']}>
					{SAMPLE_ORDERS.map((o) => (
						<tr key={o.id} className='border-t border-line'>
							<td className='py-3 pr-4 font-bold'>#{o.id}</td>
							<td className='py-3 pr-4'>{o.customer}</td>
							<td className='py-3 pr-4 text-muted'>{o.date}</td>
							<td className='py-3 pr-4 text-muted'>{o.method}</td>
							<td className='py-3 pr-4'>
								<StatusBadge status={o.status} />
							</td>
							<td className='py-3 text-right font-bold'>{formatPrice(o.total)}</td>
						</tr>
					))}
				</Table>
			</section>

			<AdminProducts
				products={products}
				lowStock={LOW_STOCK}
				onChange={onChange}
				search={search}
				onSearch={setSearch}
				page={page}
				totalPages={totalPages}
				onPage={setPage}
			/>

			{showAdd && <ProductDialog onClose={() => setShowAdd(false)} onCreated={onCreated} />}
		</div>
	);
};

const Stat = ({ icon: Icon, label, value, note, warn = false }) => (
	<div className={`card flex flex-col gap-3 p-5 ${warn ? 'border-l-4 border-brand' : ''}`}>
		<div className='flex items-center justify-between'>
			<span className='text-[12px] font-bold uppercase tracking-wider text-muted'>{label}</span>
			<span className='flex h-9 w-9 items-center justify-center rounded bg-panel text-brand'>
				<Icon size={18} />
			</span>
		</div>
		<span className={`text-[28px] font-bold leading-none ${warn ? 'text-brand' : ''}`}>{value}</span>
		<span className='text-[13px] text-muted'>{note}</span>
	</div>
);

// A plain table that scrolls sideways on small screens
const Table = ({ head, children }) => (
	<div className='-mx-6 overflow-x-auto px-6'>
		<table className='w-full min-w-[640px] text-left text-[14px]'>
			<thead>
				<tr>
					{head.map((h) => (
						<th
							key={h}
							className={`pb-3 text-[11px] font-bold uppercase tracking-wider text-muted ${h === 'Total' ? 'text-right' : ''}`}
						>
							{h}
						</th>
					))}
				</tr>
			</thead>
			<tbody>{children}</tbody>
		</table>
	</div>
);

export default AdminDashboard;
