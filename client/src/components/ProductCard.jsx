import React from 'react';
import { Link } from 'react-router-dom';
import { ShoppingBag } from 'lucide-react';

import QuantityStepper from './QuantityStepper';
import { useAddToCart } from '../hooks/useAddToCart';
import { formatPrice } from '../lib/format';
import { findCategory } from '../lib/navigation';

// One product in the grid, it can go straight into the cart and the picture or name opens the full page
const ProductCard = ({ product }) => {
	const [quantity, setQuantity] = React.useState(1);
	const { addToCart, busy, notice } = useAddToCart();
	const href = `/${product.category}/${product.slug}`;
	const soldOut = !product.inStock;

	const onAdd = async () => {
		if (await addToCart(product.id, quantity)) setQuantity(1);
	};

	return (
		<article className='card group flex flex-col overflow-hidden transition-shadow duration-300 hover:shadow-xl'>
			<Link to={href} className='relative block aspect-[4/3] overflow-hidden bg-panel' aria-label={product.name}>
				<img
					src={product.image.mobile}
					alt=''
					loading='lazy'
					className='h-full w-full object-cover transition-transform duration-500 group-hover:scale-105'
				/>
				{product.isNew && (
					<span className='absolute left-4 top-4 rounded bg-ink px-3 py-1 text-[11px] font-bold uppercase tracking-widest text-page'>
						New
					</span>
				)}
			</Link>

			<div className='flex flex-1 flex-col gap-2 p-6'>
				<p className='text-[11px] font-bold uppercase tracking-widest text-brand-dark'>{findCategory(product.category)?.label}</p>
				<Link to={href}>
					<h2 className='text-[18px] tracking-tight transition-colors group-hover:text-brand-dark'>{product.name}</h2>
				</Link>
				<p className='line-clamp-2 text-[14px] text-muted'>{product.description}</p>
				<div className='mt-auto flex items-baseline justify-between pt-2'>
					<span className='text-[24px] font-bold'>
						{formatPrice(product.price)}
					</span>
					<span className={`text-[13px] font-bold ${soldOut ? 'text-red-600' : 'text-muted'}`}>
						{soldOut ? 'Out of stock' : product.stock <= 5 ? `Only ${product.stock} left` : 'In stock'}
					</span>
				</div>
			</div>

			<div className='flex items-center gap-3 px-6 pb-2'>
				<QuantityStepper value={quantity} onChange={setQuantity} max={Math.max(1, product.stock)} label={product.name} />
				<button onClick={onAdd} disabled={soldOut || busy} className='btn-primary flex-1'>
					<ShoppingBag size={18} />
					{busy ? 'Adding...' : soldOut ? 'Sold out' : 'Add to cart'}
				</button>
			</div>
			<p role='status' className={`min-h-[36px] px-6 pb-4 pt-2 text-[13px] font-bold ${notice?.ok ? 'text-brand-dark' : 'text-red-600'}`}>
				{notice?.text}
			</p>
		</article>
	);
};

export default ProductCard;
