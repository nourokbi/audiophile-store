import React from 'react';
import { Link } from 'react-router-dom';

const Cart = (props) => {
	const cartElements = props.cart.map((item, index) => {
		const increaseQuantity = () => {
			props.updateQuantity(index, item.quantity + 1);
			props.setTotalPrice((prevPrice) => prevPrice + item.price);
		};

		const decreaseQuantity = () => {
			if (item.quantity > 1) {
				props.updateQuantity(index, item.quantity - 1);
				props.setTotalPrice((prevPrice) => prevPrice - item.price);
			} else {
				props.removeFromCart(index);
			}
		};

		return (
			<div key={index} className='w-full flex items-center justify-between'>
				<div className='flex items-center gap-4'>
					<div>
						<img src={item.image} className='w-16 h-16 rounded-[8px]' />
					</div>
					<div className='flex flex-col'>
						<p className='text-[15px] font-bold leading-[25px] text-black'>
							{item.name}
						</p>
						<p className='text-[14px] font-bold leading-[25px] text-black/50'>
							$ {item.price}
						</p>
					</div>
				</div>
				<div className='w-[96px] h-[32px] flex justify-between bg-gray-light p-[15px] items-center'>
					<div
						onClick={decreaseQuantity}
						className='text-[13px] text-black/30 hover:text-cream font-bold tracking-[1px] cursor-pointer'
					>
						-
					</div>
					<div className='text-[13px] text-black font-bold tracking-[1px]'>
						{item.quantity}
					</div>
					<div
						onClick={increaseQuantity}
						className='text-[13px] text-black/30 hover:text-cream font-bold tracking-[1px] cursor-pointer'
					>
						+
					</div>
				</div>
			</div>
		);
	});

	return (
		<div className='rounded-[8px] p-8 bg-white'>
			<div className='flex justify-between items-center'>
				<h6 className='h6 text-black'>
					Cart {props.cart.length > 0 && `(${props.cart.length})`}
				</h6>
				<button
					onClick={props.clearCart}
					className='text-[15px] font-[500] leading-[25px] underline text-black/50 hover:text-cream transition-all duration-200'
				>
					Remove all
				</button>
			</div>
			<div className='flex flex-col items-center gap-6 py-8 overflow-y-auto max-h-[304px]'>
				{props.cart.length > 0 ? (
					cartElements
				) : (
					<h6 className='text-black/20'>Cart is empty</h6>
				)}
			</div>
			{props.cart.length > 0 && (
				<div className='flex flex-col gap-6'>
					<div className='flex justify-between items-center'>
						<p className='text-[15px] font-[500] leading-[25px] text-black/50 uppercase'>
							Total
						</p>
						<h6 className='h6 text-black'>$ {props.totalPrice}</h6>
					</div>
					<div>
						{props.cart.length > 0 && (
							<Link to='/checkout'>
								<button className='btn w-full bg-cream text-white hover:bg-cream-light transition-all duration-150'>
									Checkout
								</button>
							</Link>
						)}
					</div>
				</div>
			)}
		</div>
	);
};

export default Cart;
