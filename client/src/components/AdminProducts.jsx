import React from 'react';
import { Link } from 'react-router-dom';
import { PackagePlus, Search, Tag } from 'lucide-react';

import Dialog from './Dialog';
import Pagination from './Pagination';
import { adminApi } from '../lib/adminApi';
import { formatPrice } from '../lib/format';
import { findCategory } from '../lib/navigation';

// The admin product table with search, paging, restock, change price and the available switch on each row
const AdminProducts = ({ products, lowStock, onChange, search, onSearch, page, totalPages, onPage }) => {
	const [dialog, setDialog] = React.useState(null); // { type: 'restock' or 'price', product }
	const [busyId, setBusyId] = React.useState(null);
	const [error, setError] = React.useState('');

	// Calls the API and swaps the row for the product the server answered, so the table shows what was saved
	const run = async (product, call) => {
		setBusyId(product.id);
		setError('');
		try {
			onChange(await call());
			setDialog(null);
		} catch (e) {
			setError(e.message);
		} finally {
			setBusyId(null);
		}
	};

	const toggleAvailable = (p) => run(p, () => adminApi.setAvailability(p.id, !p.isActive));

	return (
		<section className='card flex flex-col gap-4 overflow-hidden p-6'>
			<div className='flex flex-wrap items-center justify-between gap-4'>
				<h2 className='text-[18px]'>Products</h2>
				<div className='relative flex w-full items-center sm:w-72'>
					<Search size={18} className='pointer-events-none absolute left-4 text-muted' />
					<input
						type='search'
						value={search}
						onChange={(e) => onSearch(e.target.value)}
						placeholder='Search name or description'
						aria-label='Search products'
						className='input pl-11'
					/>
				</div>
			</div>
			{error && <p className='text-[14px] font-bold text-red-600'>{error}</p>}

			<div className='-mx-6 overflow-x-auto px-6'>
				<table className='w-full min-w-[820px] text-left text-[14px]'>
					<thead>
						<tr className='text-[11px] font-bold uppercase tracking-wider text-muted'>
							<th className='pb-3'>Product</th>
							<th className='pb-3'>Category</th>
							<th className='pb-3'>Price</th>
							<th className='pb-3'>Stock</th>
							<th className='pb-3'>Available</th>
							<th className='pb-3 text-right'>Actions</th>
						</tr>
					</thead>
					<tbody>
						{products.map((p) => (
							<tr key={p.id} className={`border-t border-line ${p.isActive ? '' : 'opacity-60'}`}>
								<td className='py-3 pr-4'>
									<Link to={`/${p.category}/${p.slug}`} className='flex items-center gap-3 font-bold hover:text-brand-dark'>
										<img src={p.image.mobile} alt='' className='h-10 w-10 rounded object-cover' />
										{p.name}
									</Link>
								</td>
								<td className='py-3 pr-4 text-muted'>{findCategory(p.category)?.label}</td>
								<td className='py-3 pr-4'>{formatPrice(p.price)}</td>
								<td className={`py-3 pr-4 font-bold ${p.stock <= lowStock ? 'text-brand-dark' : ''}`}>{p.stock}</td>
								<td className='py-3 pr-4'>
									<button
										role='switch'
										aria-checked={p.isActive}
										aria-label={`${p.name} available`}
										disabled={busyId === p.id}
										onClick={() => toggleAvailable(p)}
										className={`relative h-6 w-11 rounded-full transition-colors ${p.isActive ? 'bg-brand' : 'bg-panel-strong'}`}
									>
										<span
											className={`absolute left-[2px] top-[2px] h-5 w-5 rounded-full bg-white transition-transform ${
												p.isActive ? 'translate-x-5' : ''
											}`}
										/>
									</button>
								</td>
								<td className='py-3'>
									<div className='flex justify-end gap-2'>
										<button
											onClick={() => setDialog({ type: 'restock', product: p })}
											className='btn-secondary h-9 px-3 text-[11px]'
										>
											<PackagePlus size={14} /> Restock
										</button>
										<button
											onClick={() => setDialog({ type: 'price', product: p })}
											className='btn-secondary h-9 px-3 text-[11px]'
										>
											<Tag size={14} /> Price
										</button>
									</div>
								</td>
							</tr>
						))}
					</tbody>
				</table>
			</div>

			{products.length === 0 && (
				<p className='text-[14px] text-muted'>{search ? `No products match "${search}".` : 'No products yet.'}</p>
			)}
			{totalPages > 1 && <Pagination current={page} pages={totalPages} onChange={onPage} />}

			{dialog?.type === 'restock' && (
				<NumberDialog
					title={`Restock ${dialog.product.name}`}
					label='Units to add'
					initial={10}
					min={1}
					busy={busyId === dialog.product.id}
					preview={(n) => `Stock goes from ${dialog.product.stock} to ${dialog.product.stock + n}`}
					onClose={() => setDialog(null)}
					onSave={(n) => run(dialog.product, () => adminApi.restock(dialog.product.id, n))}
				/>
			)}

			{dialog?.type === 'price' && (
				<NumberDialog
					title={`Price of ${dialog.product.name}`}
					label='New price in EGP'
					initial={dialog.product.price}
					min={1}
					busy={busyId === dialog.product.id}
					preview={(n) => `Was ${formatPrice(dialog.product.price)}, becomes ${formatPrice(n)}`}
					onClose={() => setDialog(null)}
					onSave={(n) => run(dialog.product, () => adminApi.changePrice(dialog.product.id, n))}
				/>
			)}
		</section>
	);
};

// Asks for one positive number and shows what the change will do before it is saved
const NumberDialog = ({ title, label, initial, min, busy, preview, onClose, onSave }) => {
	const [value, setValue] = React.useState(String(initial));
	const number = Number(value);
	const valid = Number.isFinite(number) && number >= min;

	return (
		<Dialog title={title} onClose={onClose}>
			<form
				className='flex flex-col gap-4'
				onSubmit={(e) => {
					e.preventDefault();
					if (valid) onSave(number);
				}}
			>
				<label className='flex flex-col gap-2'>
					<span className='text-[12px] font-bold uppercase tracking-wider'>{label}</span>
					<input type='number' min={min} autoFocus value={value} onChange={(e) => setValue(e.target.value)} className='input' />
				</label>
				<p className='text-[14px] text-muted'>{valid ? preview(number) : `Enter a number of at least ${min}`}</p>
				<div className='flex justify-end gap-3'>
					<button type='button' onClick={onClose} className='btn-secondary'>
						Cancel
					</button>
					<button type='submit' disabled={!valid || busy} className='btn-primary'>
						{busy ? 'Saving...' : 'Save'}
					</button>
				</div>
			</form>
		</Dialog>
	);
};

export default AdminProducts;
