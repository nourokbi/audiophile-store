import React from 'react';
import { Link } from 'react-router-dom';

import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { formatPrice } from '../lib/format';

// The cart drop-down. Every button calls the API and shows the cart it sends back.
const Cart = ({ onClose }) => {
	const { user } = useAuth();
	const { cart, setQuantity, remove, clear } = useCart();
	const [error, setError] = React.useState('');

	const run = (action) => async () => {
		setError('');
		try {
			await action();
		} catch (e) {
			setError(e.message);
		}
	};

	if (!user) {
		return (
			<div className='rounded-[8px] p-8 bg-white'>
				<h6 className='h6 text-black'>Cart</h6>
				<p className='p text-black/50 mt-4'>Log in to add products and keep your cart on any device.</p>
				<Link to='/login' onClick={onClose}>
					<button className='btn w-full bg-cream hover:bg-cream-light transition-all duration-150 mt-6'>Log in</button>
				</Link>
			</div>
		);
	}

	return (
		<div className='rounded-[8px] p-8 bg-white'>
			<div className='flex justify-between items-center'>
				<h6 className='h6 text-black'>Cart {cart.items.length > 0 && `(${cart.totalQuantity})`}</h6>
				{cart.items.length > 0 && (
					<button
						onClick={run(clear)}
						className='text-[15px] font-[500] leading-[25px] underline text-black/50 hover:text-cream transition-all duration-200'
					>
						Remove all
					</button>
				)}
			</div>
			<div className='flex flex-col items-center gap-6 py-8 overflow-y-auto max-h-[304px]'>
				{cart.items.length === 0 && <h6 className='text-black/20'>Cart is empty</h6>}
				{cart.items.map((item) => (
					<div key={item.productId} className='w-full flex items-center justify-between gap-4'>
						<div className='flex items-center gap-4'>
							{item.imageUrl && <img src={item.imageUrl} alt='' className='w-16 h-16 rounded-[8px]' />}
							<div className='flex flex-col'>
								<p className='text-[15px] font-bold leading-[25px] text-black'>{item.productName}</p>
								<p className='text-[14px] font-bold leading-[25px] text-black/50'>{formatPrice(item.unitPrice)}</p>
							</div>
						</div>
						<div className='w-[96px] h-[32px] flex justify-between bg-gray-light p-[15px] items-center shrink-0'>
							<button
								aria-label={`One less ${item.productName}`}
								onClick={run(() =>
									item.quantity > 1 ? setQuantity(item.productId, item.quantity - 1) : remove(item.productId),
								)}
								className='text-[13px] text-black/30 hover:text-cream font-bold tracking-[1px]'
							>
								-
							</button>
							<span className='text-[13px] text-black font-bold tracking-[1px]'>{item.quantity}</span>
							<button
								aria-label={`One more ${item.productName}`}
								onClick={run(() => setQuantity(item.productId, item.quantity + 1))}
								className='text-[13px] text-black/30 hover:text-cream font-bold tracking-[1px]'
							>
								+
							</button>
						</div>
					</div>
				))}
			</div>
			{error && <p className='text-red-600 text-[14px] font-bold mb-4'>{error}</p>}
			{cart.items.length > 0 && (
				<div className='flex flex-col gap-6'>
					<div className='flex justify-between items-center'>
						<p className='text-[15px] font-[500] leading-[25px] text-black/50 uppercase'>Total</p>
						<h6 className='h6 text-black'>{formatPrice(cart.subTotal)}</h6>
					</div>
					<Link to='/checkout' onClick={onClose}>
						<button className='btn w-full bg-cream text-white hover:bg-cream-light transition-all duration-150'>
							Checkout
						</button>
					</Link>
				</div>
			)}
		</div>
	);
};

export default Cart;
