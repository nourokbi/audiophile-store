import { AlertTriangle } from 'lucide-react';

import SeeAllLink from './SeeAllLink';

// The products running out, the fewest left first, with a link to the full filtered list
const LowStockList = ({ products, lowStockLimit, seeAllTo }) => (
	<section className='card flex flex-col gap-4 p-6'>
		<div className='flex items-center justify-between gap-4'>
			<h2 className='flex items-center gap-2 text-[18px]'>
				<AlertTriangle size={18} className='text-brand' /> Low stock
			</h2>
			{seeAllTo && <SeeAllLink to={seeAllTo} />}
		</div>
		{products.length === 0 && <p className='text-[14px] text-muted'>Every product has more than {lowStockLimit} left.</p>}
		<ul className='flex flex-col gap-3'>
			{products.map((product) => (
				<li key={product.id} className='flex items-center justify-between gap-3 rounded bg-panel p-3'>
					<div className='flex min-w-0 items-center gap-3'>
						<img src={product.image.mobile} alt='' className='h-10 w-10 rounded object-cover' />
						<span className='truncate text-[14px] font-bold'>{product.name}</span>
					</div>
					<span className='shrink-0 text-[13px] font-bold text-brand-dark'>{product.stock} left</span>
				</li>
			))}
		</ul>
	</section>
);

export default LowStockList;
