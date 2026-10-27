import { Link } from 'react-router-dom';
import { PackagePlus, Pencil, Tag } from 'lucide-react';

import Pagination from '../ui/Pagination';
import Switch from '../ui/Switch';
import { formatPrice } from '../../lib/format';
import { findCategory } from '../../lib/navigation';
import { productPath } from '../../lib/productHelpers';

// The product table and its pages, every button only reports back to the products page
const ProductsTable = ({
	products,
	lowStockLimit,
	savingProductId,
	errorMessage,
	totalCount,
	hasFilters,
	currentPage,
	totalPages,
	onPageChange,
	onToggleAvailability,
	onEditClick,
	onRestockClick,
	onPriceClick,
}) => (
	<section className='card flex flex-col gap-4 overflow-hidden p-6'>
		<div className='flex flex-wrap items-center justify-between gap-4'>
			<h2 className='text-[18px]'>Products</h2>
			<span className='text-[14px] text-muted'>
				{totalCount} {totalCount === 1 ? 'product' : 'products'}
			</span>
		</div>
		{errorMessage && <p className='error-text'>{errorMessage}</p>}

		<div className='-mx-6 overflow-x-auto px-6'>
			<table className='w-full min-w-[920px] text-left text-[14px]'>
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
								<Link to={productPath(product)} className='flex items-center gap-3 font-bold hover:text-brand-dark'>
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
								<Switch
									isOn={product.isActive}
									label={`${product.name} available`}
									disabled={savingProductId === product.id}
									onClick={() => onToggleAvailability(product)}
								/>
							</td>
							<td className='py-3'>
								<div className='flex justify-end gap-2'>
									<button onClick={() => onEditClick(product)} className='btn-secondary h-9 px-3 text-[11px]'>
										<Pencil size={14} /> Edit
									</button>
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
			<p className='text-[14px] text-muted'>{hasFilters ? 'No product matches these filters.' : 'No products yet.'}</p>
		)}
		{totalPages > 1 && <Pagination currentPage={currentPage} totalPages={totalPages} onPageChange={onPageChange} />}
	</section>
);

export default ProductsTable;
