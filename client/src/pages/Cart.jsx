import React from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight, Check, LogIn, ShoppingBag, Trash2 } from 'lucide-react';

import PriceSummary from '../components/shop/PriceSummary';
import EmptyState from '../components/ui/EmptyState';
import QuantityStepper from '../components/ui/QuantityStepper';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { useCatalog } from '../context/CatalogContext';
import { formatPrice } from '../lib/format';
import { findCategory } from '../lib/navigation';
import { isFewLeft, productPath, stockLabel } from '../lib/productHelpers';
import { SHIPPING } from '../lib/shipping';

// The cart page, every change goes to the API and the page shows the cart it sends back
const Cart = () => {
	const { user } = useAuth();
	const { cart, setQuantity, remove, clear } = useCart();
	const { products } = useCatalog();
	const [changeError, setChangeError] = React.useState('');

	const runCartChange = async (sendChange) => {
		setChangeError('');
		try {
			await sendChange();
		} catch (error) {
			setChangeError(error.message);
		}
	};

	if (!user || cart.items.length === 0) {
		return (
			<div className='page py-16'>
				<EmptyState
					icon={ShoppingBag}
					title={user ? 'Your cart is empty' : 'Log in to see your cart'}
					text={
						user
							? 'Add something you like and it will wait here until you check out.'
							: 'Your cart is saved on your account, so it is the same on every device.'
					}
				>
					{user ? (
						<Link to='/' className='btn-primary'>
							Continue shopping
						</Link>
					) : (
						<Link to='/login?redirect=%2Fcart' className='btn-primary'>
							<LogIn size={16} /> Log in
						</Link>
					)}
				</EmptyState>
			</div>
		);
	}

	const leftForFreeShipping = SHIPPING.freeFrom - cart.subTotal;
	const itemCountText = `${cart.totalQuantity} ${cart.totalQuantity === 1 ? 'item' : 'items'}`;

	return (
		<div className='page flex flex-col gap-8 pb-24 pt-10'>
			<div className='flex flex-wrap items-end justify-between gap-4'>
				<div>
					<p className='eyebrow'>Your selection</p>
					<h1 className='title mt-2'>Cart</h1>
				</div>
				<button onClick={() => runCartChange(clear)} className='label flex items-center gap-2 text-muted hover:text-red-600'>
					<Trash2 size={14} /> Remove all
				</button>
			</div>

			<div className='grid grid-cols-1 items-start gap-8 lg:grid-cols-3'>
				<section className='flex flex-col gap-4 lg:col-span-2'>
					{cart.items.map((item) => (
						<CartItem
							key={item.productId}
							item={item}
							product={products.find((product) => product.id === item.productId)}
							onQuantityChange={(quantity) => runCartChange(() => setQuantity(item.productId, quantity))}
							onRemove={() => runCartChange(() => remove(item.productId))}
						/>
					))}
				</section>

				<aside className='card flex flex-col gap-5 p-6 lg:sticky lg:top-28'>
					<h2 className='text-[18px]'>Summary</h2>
					<PriceSummary
						subTotal={cart.subTotal}
						shippingFee={cart.shippingFee}
						total={cart.total}
						subTotalLabel={`Subtotal, ${itemCountText}`}
					/>
					{leftForFreeShipping > 0 && (
						<p className='text-[13px] text-muted'>Add {formatPrice(leftForFreeShipping)} more for free shipping.</p>
					)}
					{changeError && <p className='error-text'>{changeError}</p>}
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
const CartItem = ({ item, product, onQuantityChange, onRemove }) => {
	const productLink = product ? productPath(product) : '#';
	const imageUrl = product?.image.mobile ?? item.imageUrl;

	return (
		<article className='card flex flex-col gap-5 p-5 sm:flex-row'>
			<Link to={productLink} className='shrink-0 overflow-hidden rounded bg-panel sm:w-44'>
				{imageUrl && <img src={imageUrl} alt={item.productName} className='aspect-square w-full object-cover' />}
			</Link>

			<div className='flex min-w-0 flex-1 flex-col gap-3'>
				<div className='flex items-start justify-between gap-4'>
					<div className='min-w-0'>
						{product && <p className='eyebrow text-[11px]'>{findCategory(product.category)?.label}</p>}
						<Link to={productLink} className='mt-1 block text-[18px] font-bold uppercase hover:text-brand-dark'>
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
						{product.inStock && !isFewLeft(product) ? (
							<span className='flex items-center gap-1 text-emerald-600'>
								<Check size={14} /> {stockLabel(product)}
							</span>
						) : (
							<span className='text-brand-dark'>{stockLabel(product)}</span>
						)}
						<span className='text-muted'>- {formatPrice(item.unitPrice)} each</span>
					</p>
				)}

				<div className='mt-auto flex items-center justify-between gap-4 pt-2'>
					<QuantityStepper
						value={item.quantity}
						max={Math.max(item.quantity, product?.stock ?? 100)}
						label={item.productName}
						onChange={onQuantityChange}
					/>
					<p className='text-[20px] font-bold'>{formatPrice(item.lineTotal)}</p>
				</div>
			</div>
		</article>
	);
};

export default Cart;
