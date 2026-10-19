import React from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight, LogIn, ShoppingBag, Trash2 } from 'lucide-react';

import QuantityStepper from '../components/QuantityStepper';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { formatPrice } from '../lib/format';
import { SHIPPING } from '../lib/shipping';

// The cart page, every change goes to the API and the page shows the cart it sends back
const Cart = () => {
	const { user } = useAuth();
	const { cart, setQuantity, remove, clear } = useCart();
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
				<section className='card flex flex-col divide-y divide-line lg:col-span-2'>
					{cart.items.map((item) => (
						<div key={item.productId} className='flex flex-wrap items-center gap-4 p-5 sm:flex-nowrap'>
							{item.imageUrl && <img src={item.imageUrl} alt='' className='h-20 w-20 shrink-0 rounded bg-panel object-cover' />}
							<div className='min-w-0 flex-1'>
								<p className='truncate text-[16px] font-bold'>{item.productName}</p>
								<p className='text-[14px] text-muted'>{formatPrice(item.unitPrice)} each</p>
							</div>
							<QuantityStepper
								value={item.quantity}
								label={item.productName}
								onChange={(quantity) => run(() => setQuantity(item.productId, quantity))}
							/>
							<p className='w-28 text-right text-[16px] font-bold'>{formatPrice(item.lineTotal)}</p>
							<button
								aria-label={`Remove ${item.productName}`}
								onClick={() => run(() => remove(item.productId))}
								className='rounded p-2 text-muted hover:bg-panel hover:text-red-600'
							>
								<Trash2 size={18} />
							</button>
						</div>
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

const Row = ({ label, value }) => (
	<div className='flex items-center justify-between'>
		<span className='text-muted'>{label}</span>
		<span className='font-bold'>{value}</span>
	</div>
);

export default Cart;
