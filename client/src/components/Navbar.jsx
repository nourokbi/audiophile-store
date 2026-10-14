import React from 'react';
import { Link } from 'react-router-dom';

import logo from '/assets/shared/desktop/logo.svg';
import hamburger from '/assets/shared/tablet/icon-hamburger.svg';

import Menu from './Menu';
import Cart from './Cart';

const Navbar = (props) => {
	const [showMenu, setShowMenu] = React.useState(false);
	const [showCart, setShowCart] = React.useState(false);

	function toggleMenu() {
		setShowMenu((prev) => !prev);
	}

	function toggleCart() {
		setShowCart((prev) => !prev);
	}

	React.useEffect(() => {
		function handleResize() {
			if (window.innerWidth > 975) {
				setShowMenu(false);
			}
		}

		window.addEventListener('resize', handleResize);

		return () => {
			window.removeEventListener('resize', handleResize);
		};
	}, []);

	return (
		<>
			<nav className='absolute top-0 left-0 right-0 xs:px-[24px] md:px-[40px] lg:px-[165px] bg-stone z-50'>
				<div className='flex justify-between items-center py-[35px]'>
					<div className='flex items-center xs:justify-between md:justify-normal md:gap-[42px]'>
						<img
							onClick={toggleMenu}
							src={hamburger}
							className='lg:hidden cursor-pointer'
						/>
						{/* Logo when screen-size is larger than xs */}
						<Link to='/' className='xs:hidden md:flex'>
							<img src={logo} />
						</Link>
					</div>
					{/* Logo when screen-size is xs */}
					<Link to='/' className='xs:flex md:hidden'>
						<img src={logo} />
					</Link>
					<div className='text-white xs:hidden lg:flex text-[13px] uppercase font-bold leading-[25px] tracking-[2px] gap-[34px]'>
						<Link
							to='/'
							className='hover:text-cream transition-all duration-200'
						>
							Home
						</Link>
						<Link
							to='/headphones'
							className='hover:text-cream transition-all duration-200'
						>
							Headphones
						</Link>
						<Link
							to='/speakers'
							className='hover:text-cream transition-all duration-200'
						>
							Speakers
						</Link>
						<Link
							to='/earphones'
							className='hover:text-cream transition-all duration-200'
						>
							Earphones
						</Link>
					</div>
					<div onClick={toggleCart} className='relative cursor-pointer'>
						{props.cart.length > 0 && (
							<div className='absolute top-[-10px] right-[-10px] rounded-full bg-cream flex items-center justify-center h-[20px] w-[20px]'>
								<p className='text-white font-bold text-[12px]'>
									{props.cart.length}
								</p>
							</div>
						)}
						<svg
							width='23'
							height='20'
							xmlns='http://www.w3.org/2000/svg'
							className='fill-white hover:fill-cream transition-all duration-200'
						>
							<path
								d='M8.625 15.833c1.132 0 2.054.935 2.054 2.084 0 1.148-.922 2.083-2.054 2.083-1.132 0-2.054-.935-2.054-2.083 0-1.15.922-2.084 2.054-2.084zm9.857 0c1.132 0 2.054.935 2.054 2.084 0 1.148-.922 2.083-2.054 2.083-1.132 0-2.053-.935-2.053-2.083 0-1.15.92-2.084 2.053-2.084zm-9.857 1.39a.69.69 0 00-.685.694.69.69 0 00.685.694.69.69 0 00.685-.694.69.69 0 00-.685-.695zm9.857 0a.69.69 0 00-.684.694.69.69 0 00.684.694.69.69 0 00.685-.694.69.69 0 00-.685-.695zM4.717 0c.316 0 .59.215.658.517l.481 2.122h16.47a.68.68 0 01.538.262c.127.166.168.38.11.579l-2.695 9.236a.672.672 0 01-.648.478H7.41a.667.667 0 00-.673.66c0 .364.303.66.674.66h12.219c.372 0 .674.295.674.66 0 .364-.302.66-.674.66H7.412c-1.115 0-2.021-.889-2.021-1.98 0-.812.502-1.511 1.218-1.816L4.176 1.32H.674A.667.667 0 010 .66C0 .296.302 0 .674 0zm16.716 3.958H6.156l1.797 7.917h11.17l2.31-7.917z'
								fillRule='nonzero'
							/>
						</svg>
					</div>
				</div>
			</nav>
			{showMenu && (
				<>
					<div className='fixed top-0 bottom-0 left-0 right-0 bg-black/50 z-[1]' />
					<div className='absolute left-0 right-0 z-40'>
						<Menu />
					</div>
				</>
			)}
			{showCart && (
				<>
					<div className='fixed top-0 bottom-0 left-0 right-0 bg-black/50 z-[1]' />
					<div className='md:w-[377px] absolute xs:left-6 xs:right-6 md:left-auto md:right-10 lg:right-40 top-[120px] z-30'>
						<Cart
							cart={props.cart}
							clearCart={props.clearCart}
							totalPrice={props.totalPrice}
							setTotalPrice={props.setTotalPrice}
							updateQuantity={props.updateQuantity}
							removeFromCart={props.removeFromCart}
						/>
					</div>
				</>
			)}
		</>
	);
};

export default Navbar;
