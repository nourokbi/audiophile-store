import React from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { LogOut, Menu, Package, ShoppingBag, User, X } from 'lucide-react';

import Cart from './Cart';
import Logo from './Logo';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { NAV_LINKS, navLinkClass } from '../lib/navigation';

// The fixed header from the Stitch design, with the cart panel and the account menu
const Navbar = () => {
	const { user, logout } = useAuth();
	const { cart } = useCart();
	const navigate = useNavigate();
	const [open, setOpen] = React.useState(null); // 'menu', 'cart', 'account' or null

	const toggle = (panel) => setOpen((current) => (current === panel ? null : panel));
	const close = () => setOpen(null);

	const onLogout = async () => {
		close();
		await logout();
		navigate('/');
	};

	return (
		<header className='fixed inset-x-0 top-0 z-50 bg-white/90 shadow-[0_1px_8px_rgba(0,0,0,0.04)] backdrop-blur-md'>
			<div className='page flex h-20 items-center justify-between gap-4'>
				<div className='flex items-center gap-8'>
					<button className='lg:hidden' aria-label='Menu' onClick={() => toggle('menu')}>
						{open === 'menu' ? <X size={24} /> : <Menu size={24} />}
					</button>
					<Logo />
					<nav className='hidden items-center gap-6 lg:flex'>
						{NAV_LINKS.map((link) => (
							<NavLink key={link.to} to={link.to} end className={navLinkClass}>
								{link.label}
							</NavLink>
						))}
					</nav>
				</div>

				<div className='flex items-center gap-5'>
					<button
						aria-label='Cart'
						onClick={() => toggle('cart')}
						className='relative p-2 transition-colors hover:text-brand-dark'
					>
						<ShoppingBag size={24} />
						{cart.totalQuantity > 0 && (
							<span className='absolute -right-1 -top-1 flex h-5 min-w-[20px] items-center justify-center rounded-full bg-brand px-1 text-[12px] font-bold text-white'>
								{cart.totalQuantity}
							</span>
						)}
					</button>

					{user ? (
						<div className='relative'>
							<button onClick={() => toggle('account')} className='flex items-center gap-3'>
								<span className='hidden text-right sm:block'>
									<span className='label block normal-case'>{user.name}</span>
									<span className='block text-[10px] font-bold uppercase tracking-widest text-brand-dark'>
										{user.roles?.includes('Admin') ? 'Admin' : 'Customer'}
									</span>
								</span>
								<span className='flex h-9 w-9 items-center justify-center rounded-full bg-brand-dark text-white'>
									<User size={18} />
								</span>
							</button>
							{open === 'account' && (
								<div className='card absolute right-0 top-12 flex min-w-[180px] flex-col p-2 shadow-lg'>
									<Link to='/orders' onClick={close} className='label flex items-center gap-2 rounded px-3 py-2 hover:bg-panel'>
										<Package size={16} /> My orders
									</Link>
									<button onClick={onLogout} className='label flex items-center gap-2 rounded px-3 py-2 text-muted hover:bg-panel'>
										<LogOut size={16} /> Log out
									</button>
								</div>
							)}
						</div>
					) : (
						<Link to='/login' className='btn-primary h-10 px-5'>
							Log in
						</Link>
					)}
				</div>
			</div>

			{open === 'menu' && (
				<nav className='page flex flex-col gap-4 border-t border-line py-6 lg:hidden'>
					{NAV_LINKS.map((link) => (
						<NavLink key={link.to} to={link.to} end onClick={close} className={navLinkClass}>
							{link.label}
						</NavLink>
					))}
				</nav>
			)}

			{open === 'cart' && (
				<>
					<div className='fixed inset-0 top-20 -z-10 bg-black/40' onClick={close} />
					<div className='page relative'>
						<div className='absolute right-6 top-4 w-[calc(100%-48px)] md:right-10 md:w-[400px] lg:right-0'>
							<Cart onClose={close} />
						</div>
					</div>
				</>
			)}
		</header>
	);
};

export default Navbar;
