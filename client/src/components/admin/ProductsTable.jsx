import { Link } from 'react-router-dom';
import { PackagePlus, Search, Tag } from 'lucide-react';

import Pagination from '../Pagination';
import { formatPrice } from '../../lib/format';
import { findCategory } from '../../lib/navigation';

// The product table with its search box and pages, every button only reports back to the dashboard
const ProductsTable = ({
	products,
	lowStockLimit,
	savingProductId,
	errorMessage,
	searchText,
	onSearchTextChange,
	currentPage,
	totalPages,
	onPageChange,
	onToggleAvailability,
	onRestockClick,
	onPriceClick,
}) => (
	<section className='card flex flex-col gap-4 overflow-hidden p-6'>
		<div className='flex flex-wrap items-center justify-between gap-4'>
			<h2 className='text-[18px]'>Products</h2>
			<div className='relative flex w-full items-center sm:w-72'>
				<Search size={18} className='pointer-events-none absolute left-4 text-muted' />
				<input
					type='search'
					value={searchText}
					onChange={(event) => onSearchTextChange(event.target.value)}
					placeholder='Search name or description'
					aria-label='Search products'
					className='input pl-11'
				/>
			</div>
		</div>
		{errorMessage && <p className='text-[14px] font-bold text-red-600'>{errorMessage}</p>}

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
					{products.map((product) => (
						<tr key={product.id} className={`border-t border-line ${product.isActive ? '' : 'opacity-60'}`}>
							<td className='py-3 pr-4'>
								<Link
									to={`/${product.category}/${product.slug}`}
									className='flex items-center gap-3 font-bold hover:text-brand-dark'
								>
									<img src={product.image.mobile} alt='' className='h-10 w-10 rounded object-cover' />
									{product.name}
								</Link>
							</td>
							<td className='py-3 pr-4 text-muted'>{findCategory(product.category)?.label}</td>
							<td className='py-3 pr-4'>{formatPrice(product.price)}</td>
							<td className={`py-3 pr-4 font-bold ${product.stock <= lowStockLimit ? 'text-brand-dark' : ''}`}>
								{product.stock}
							</td>
							<td className='py-3 pr-4'>
								<AvailabilitySwitch
									isAvailable={product.isActive}
									label={`${product.name} available`}
									disabled={savingProductId === product.id}
									onClick={() => onToggleAvailability(product)}
								/>
							</td>
							<td className='py-3'>
								<div className='flex justify-end gap-2'>
									<button onClick={() => onRestockClick(product)} className='btn-secondary h-9 px-3 text-[11px]'>
										<PackagePlus size={14} /> Restock
									</button>
									<button onClick={() => onPriceClick(product)} className='btn-secondary h-9 px-3 text-[11px]'>
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
			<p className='text-[14px] text-muted'>{searchText ? `No products match "${searchText}".` : 'No products yet.'}</p>
		)}
		{totalPages > 1 && <Pagination current={currentPage} pages={totalPages} onChange={onPageChange} />}
	</section>
);

const AvailabilitySwitch = ({ isAvailable, label, disabled, onClick }) => (
	<button
		role='switch'
		aria-checked={isAvailable}
		aria-label={label}
		disabled={disabled}
		onClick={onClick}
		className={`relative h-6 w-11 rounded-full transition-colors ${isAvailable ? 'bg-brand' : 'bg-panel-strong'}`}
	>
		<span
			className={`absolute left-[2px] top-[2px] h-5 w-5 rounded-full bg-white transition-transform ${isAvailable ? 'translate-x-5' : ''}`}
		/>
	</button>
);

export default ProductsTable;
