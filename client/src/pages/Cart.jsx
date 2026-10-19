import React from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight, Check, LogIn, ShoppingBag, Trash2 } from 'lucide-react';

import QuantityStepper from '../components/QuantityStepper';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { useCatalog } from '../context/CatalogContext';
import { formatPrice } from '../lib/format';
import { findCategory } from '../lib/navigation';
import { SHIPPING } from '../lib/shipping';

// The cart page, every change goes to the API and the page shows the cart it sends back
const Cart = () => {
	const { user } = useAuth();
	const { cart, setQuantity, remove, clear } = useCart();
	const { products } = useCatalog();
	const [error, setError] = React.useState('');

	const run = async (action) => {
		setError('');
		try {
			await action();
		} catch (e) {
			setError(e.message);
		}
	};

	if (!user || cart.items.length === 0) {
		return (
			<div className='page py-16'>
				<div className='card flex flex-col items-center gap-4 p-12 text-center'>
					<ShoppingBag size={36} className='text-muted' />
					<h1 className='text-[24px]'>{user ? 'Your cart is empty' : 'Log in to see your cart'}</h1>
					<p className='max-w-md text-muted'>
						{user
							? 'Add something you like and it will wait here until you check out.'
							: 'Your cart is saved on your account, so it is the same on every device.'}
					</p>
					<Link to={user ? '/' : '/login?redirect=%2Fcart'} className='btn-primary'>
						{user ? 'Continue shopping' : (<><LogIn size={16} /> Log in</>)}
					</Link>
				</div>
			</div>
		);
	}

	const toFreeShipping = SHIPPING.freeFrom - cart.subTotal;

	return (
		<div className='page flex flex-col gap-8 pb-24 pt-10'>
			<div className='flex flex-wrap items-end justify-between gap-4'>
				<div>
					<p className='eyebrow'>Your selection</p>
					<h1 className='title mt-2'>Cart</h1>
				</div>
				<button onClick={() => run(clear)} className='label flex items-center gap-2 text-muted hover:text-red-600'>
					<Trash2 size={14} /> Remove all
				</button>
			</div>

			<div className='grid grid-cols-1 items-start gap-8 lg:grid-cols-3'>
				<section className='flex flex-col gap-4 lg:col-span-2'>
					{cart.items.map((item) => (
						<CartItem
							key={item.productId}
							item={item}
							product={products.find((p) => p.id === item.productId)}
							onQuantity={(quantity) => run(() => setQuantity(item.productId, quantity))}
							onRemove={() => run(() => remove(item.productId))}
						/>
					))}
				</section>

				<aside className='card flex flex-col gap-5 p-6 lg:sticky lg:top-28'>
					<h2 className='text-[18px]'>Summary</h2>
					<div className='flex flex-col gap-2 rounded bg-panel p-4 text-[15px]'>
						<Row label={`Subtotal, ${cart.totalQuantity} ${cart.totalQuantity === 1 ? 'item' : 'items'}`} value={formatPrice(cart.subTotal)} />
						<Row label='Shipping' value={cart.shippingFee > 0 ? formatPrice(cart.shippingFee) : 'Free'} />
						<div className='mt-2 flex items-center justify-between border-t border-line pt-3'>
							<span className='label'>Total</span>
							<span className='text-[24px] font-bold text-brand'>{formatPrice(cart.total)}</span>
						</div>
					</div>
					{toFreeShipping > 0 && (
						<p className='text-[13px] text-muted'>
							Add {formatPrice(toFreeShipping)} more for free shipping.
						</p>
					)}
					{error && <p className='text-[14px] font-bold text-red-600'>{error}</p>}
					<Link to='/checkout' className='btn-primary w-full'>
						Checkout <ArrowRight size={16} />
					</Link>
					<Link to='/' className='btn-secondary w-full'>
						Continue shopping
					</Link>
				</aside>
			</div>
		</div>
	);
};

// One cart line as a card, the catalogue adds the category, description, stock and the link to the product
const CartItem = ({ item, product, onQuantity, onRemove }) => {
	const href = product ? `/${product.category}/${product.slug}` : null;
	const image = product?.image.mobile ?? item.imageUrl;

	return (
		<article className='card flex flex-col gap-5 p-5 sm:flex-row'>
			<Link to={href ?? '#'} className='shrink-0 overflow-hidden rounded bg-panel sm:w-44'>
				{image && <img src={image} alt={item.productName} className='aspect-square w-full object-cover' />}
			</Link>

			<div className='flex min-w-0 flex-1 flex-col gap-3'>
				<div className='flex items-start justify-between gap-4'>
					<div className='min-w-0'>
						{product && <p className='eyebrow text-[11px]'>{findCategory(product.category)?.label}</p>}
						<Link to={href ?? '#'} className='mt-1 block text-[18px] font-bold uppercase hover:text-brand-dark'>
							{item.productName}
						</Link>
					</div>
					<button
						aria-label={`Remove ${item.productName}`}
						onClick={onRemove}
						className='rounded p-2 text-muted hover:bg-panel hover:text-red-600'
					>
						<Trash2 size={18} />
					</button>
				</div>

				{product && <p className='line-clamp-2 text-[14px] text-muted'>{product.description}</p>}

				{product && (
					<p className='flex items-center gap-2 text-[13px] font-bold'>
						{product.stock > 5 ? (
							<span className='flex items-center gap-1 text-emerald-600'>
								<Check size={14} /> In stock
							</span>
						) : (
							<span className='text-brand-dark'>Only {product.stock} left</span>
						)}
						<span className='text-muted'>- {formatPrice(item.unitPrice)} each</span>
					</p>
				)}

				<div className='mt-auto flex items-center justify-between gap-4 pt-2'>
					<QuantityStepper
						value={item.quantity}
						max={Math.max(item.quantity, product?.stock ?? 100)}
						label={item.productName}
						onChange={onQuantity}
					/>
					<p className='text-[20px] font-bold'>{formatPrice(item.lineTotal)}</p>
				</div>
			</div>
		</article>
	);
};

const Row = ({ label, value }) => (
	<div className='flex items-center justify-between'>
		<span className='text-muted'>{label}</span>
		<span className='font-bold'>{value}</span>
	</div>
);

export default Cart;
