import React from 'react';
import { Link } from 'react-router-dom';
import { ShoppingBag, Trash2 } from 'lucide-react';

import QuantityStepper from './QuantityStepper';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { formatPrice } from '../lib/format';

// The cart panel under the header, every change goes to the API and shows the cart it sends back
const Cart = ({ onClose }) => {
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

	if (!user) {
		return (
			<div className='card p-6 shadow-xl'>
				<h3 className='text-[18px]'>Cart</h3>
				<p className='mt-3 text-[15px] text-muted'>Log in to add products and keep your cart on any device.</p>
				<Link to='/login' onClick={onClose} className='btn-primary mt-6 w-full'>
					Log in
				</Link>
			</div>
		);
	}

	return (
		<div className='card p-6 shadow-xl'>
			<div className='flex items-center justify-between'>
				<h3 className='text-[18px]'>Cart ({cart.totalQuantity})</h3>
				{cart.items.length > 0 && (
					<button onClick={() => run(clear)} className='flex items-center gap-1 text-[14px] text-muted hover:text-red-600'>
						<Trash2 size={14} /> Remove all
					</button>
				)}
			</div>

			{cart.items.length === 0 ? (
				<div className='flex flex-col items-center gap-3 py-10 text-muted'>
					<ShoppingBag size={32} />
					<p className='text-[15px]'>Your cart is empty</p>
				</div>
			) : (
				<ul className='flex max-h-[320px] flex-col gap-4 overflow-y-auto py-6'>
					{cart.items.map((item) => (
						<li key={item.productId} className='flex items-center justify-between gap-3'>
							<div className='flex min-w-0 items-center gap-3'>
								{item.imageUrl && <img src={item.imageUrl} alt='' className='h-14 w-14 rounded bg-panel' />}
								<div className='min-w-0'>
									<p className='truncate text-[15px] font-bold'>{item.productName}</p>
									<p className='text-[14px] font-bold text-muted'>{formatPrice(item.unitPrice)}</p>
								</div>
							</div>
							<QuantityStepper
								small
								value={item.quantity}
								label={item.productName}
								onChange={(quantity) => run(() => setQuantity(item.productId, quantity))}
							/>
							<button
								aria-label={`Remove ${item.productName}`}
								onClick={() => run(() => remove(item.productId))}
								className='text-muted hover:text-red-600'
							>
								<Trash2 size={16} />
							</button>
						</li>
					))}
				</ul>
			)}

			{error && <p className='mb-4 text-[14px] font-bold text-red-600'>{error}</p>}

			{cart.items.length > 0 && (
				<>
					<div className='flex items-center justify-between border-t border-line pt-4'>
						<span className='label text-muted'>Subtotal</span>
						<span className='text-[18px] font-bold'>{formatPrice(cart.subTotal)}</span>
					</div>
					<Link to='/checkout' onClick={onClose} className='btn-primary mt-6 w-full'>
						Checkout
					</Link>
				</>
			)}
		</div>
	);
};

export default Cart;
