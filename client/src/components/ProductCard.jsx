import React from 'react';
import { Link } from 'react-router-dom';

import QuantityStepper from './QuantityStepper';
import { useAddToCart } from '../hooks/useAddToCart';
import { formatPrice } from '../lib/format';

// One product on the home page: picture, name, price and stock from the API, and a way to
// add it to the cart without opening the product. The picture and name open the full page.
// `product` is the data.json entry (look); `live` is the API's product (price, stock, id).
const ProductCard = ({ product, live, loading }) => {
	const [quantity, setQuantity] = React.useState(1);
	const { addToCart, busy, notice } = useAddToCart();
	const href = `/${product.category}/${product.slug}`;
	const soldOut = live && !live.inStock;

	const onAdd = async () => {
		if (await addToCart(live.id, quantity)) setQuantity(1);
	};

	return (
		<article className='flex flex-col bg-white rounded-[8px] overflow-hidden border border-transparent hover:border-cream/40 transition-all duration-150'>
			<Link to={href} className='block bg-gray-light aspect-square overflow-hidden' aria-label={product.name}>
				<img
					src={product.image.mobile}
					alt=''
					loading='lazy'
					className='w-full h-full object-cover hover:scale-[1.03] transition-transform duration-300'
				/>
			</Link>

			<div className='flex flex-col flex-1 p-6 gap-3'>
				<div className='flex items-center justify-between gap-3 min-h-[20px]'>
					<p className='text-[12px] text-black/40 font-bold uppercase tracking-[2px]'>{product.category}</p>
					{product.new && (
						<p className='text-[12px] text-cream font-bold uppercase tracking-[2px]'>New</p>
					)}
				</div>

				<Link to={href} className='hover:text-cream transition-all duration-150'>
					<h6 className='text-[18px] leading-[24px]'>{product.name}</h6>
				</Link>
				<p className='p text-black/50 line-clamp-2'>{product.description}</p>

				<div className='flex items-baseline justify-between gap-3 mt-auto pt-2'>
					<p className='text-[18px] font-bold tracking-[1.3px] text-black tabular-nums'>
						{live ? formatPrice(live.price) : loading ? '…' : 'Not available'}
					</p>
					<p className={`text-[13px] font-bold ${soldOut ? 'text-red-600' : 'text-black/40'}`}>
						{!live ? '' : soldOut ? 'Out of stock' : live.stock <= 5 ? `Only ${live.stock} left` : 'In stock'}
					</p>
				</div>

				<div className='flex gap-3'>
					<QuantityStepper
						value={quantity}
						onChange={setQuantity}
						max={Math.max(1, live?.stock ?? 1)}
						label={product.name}
					/>
					<button
						onClick={onAdd}
						disabled={!live || soldOut || busy}
						className='btn flex-1 px-4 bg-cream hover:bg-cream-light transition-all duration-150 whitespace-nowrap disabled:opacity-50 disabled:cursor-not-allowed'
					>
						{busy ? 'Adding…' : soldOut ? 'Sold out' : 'Add to cart'}
					</button>
				</div>

				<p
					role='status'
					className={`text-[13px] font-bold min-h-[20px] ${notice?.ok ? 'text-cream' : 'text-red-600'}`}
				>
					{notice?.text}
				</p>
			</div>
		</article>
	);
};

export default ProductCard;
