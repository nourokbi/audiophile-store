import React from 'react';
import { Link, NavLink } from 'react-router-dom';
import { ChevronDown, Menu, ShoppingBag, User, X } from 'lucide-react';

import Logo from './Logo';
import UserDrawer from './UserDrawer';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { CATEGORIES, navLinkClass } from '../lib/navigation';

// The fixed header, the links sit in the middle and the cart and account drawer on the right
const Navbar = () => {
	const { user } = useAuth();
	const { cart } = useCart();
	const [open, setOpen] = React.useState(null); // 'menu', 'account' or null

	const toggle = (panel) => setOpen((current) => (current === panel ? null : panel));
	const close = () => setOpen(null);

	return (
		<header className='fixed inset-x-0 top-0 z-50 bg-surface/90 shadow-[0_1px_8px_rgba(0,0,0,0.06)] backdrop-blur-md'>
			<div className='page flex h-20 items-center justify-between gap-4 lg:grid lg:grid-cols-[1fr_auto_1fr]'>
				<div className='flex items-center gap-4'>
					<button className='lg:hidden' aria-label='Menu' onClick={() => toggle('menu')}>
						{open === 'menu' ? <X size={24} /> : <Menu size={24} />}
					</button>
					<Logo />
				</div>

				<nav className='hidden items-center gap-8 lg:flex'>
					<NavLink to='/' end className={navLinkClass}>
						Home
					</NavLink>
					<CategoriesMenu />
					<NavLink to='/new' className={navLinkClass}>
						New collection
					</NavLink>
				</nav>

				<div className='flex items-center justify-end gap-3'>
					<Link to='/cart' aria-label='Cart' onClick={close} className='relative rounded p-2 transition-colors hover:bg-panel'>
						<ShoppingBag size={22} />
						{cart.totalQuantity > 0 && (
							<span className='absolute -right-1 -top-1 flex h-5 min-w-[20px] items-center justify-center rounded-full bg-brand px-1 text-[12px] font-bold text-white'>
								{cart.totalQuantity}
							</span>
						)}
					</Link>
					<button
						aria-label='Account'
						onClick={() => toggle('account')}
						className='flex h-10 w-10 items-center justify-center rounded-full bg-brand text-white transition-colors hover:bg-brand-hover'
					>
						{user ? <span className='text-[15px] font-bold'>{user.name.charAt(0).toUpperCase()}</span> : <User size={18} />}
					</button>
				</div>
			</div>

			{open === 'menu' && (
				<nav className='page flex flex-col gap-1 border-t border-line py-4 lg:hidden'>
					<NavLink to='/' end onClick={close} className='menu-item'>
						Home
					</NavLink>
					<NavLink to='/new' onClick={close} className='menu-item'>
						New collection
					</NavLink>
					{CATEGORIES.map(({ key, label, icon: Icon }) => (
						<NavLink key={key} to={`/${key}`} onClick={close} className='menu-item text-muted'>
							<Icon size={18} /> {label}
						</NavLink>
					))}
				</nav>
			)}

			{open === 'account' && <UserDrawer onClose={close} />}
		</header>
	);
};

// Opens on hover with a mouse and on click with touch or keyboard
const CategoriesMenu = () => (
	<div className='group relative'>
		<button className='label flex items-center gap-1 text-muted transition-colors hover:text-ink group-focus-within:text-ink'>
			Categories <ChevronDown size={14} className='transition-transform group-hover:rotate-180' />
		</button>
		<div className='invisible absolute left-1/2 top-full w-64 -translate-x-1/2 pt-4 opacity-0 transition-all group-focus-within:visible group-focus-within:opacity-100 group-hover:visible group-hover:opacity-100'>
			<div className='card flex flex-col p-2 shadow-xl'>
				{CATEGORIES.map(({ key, label, icon: Icon }) => (
					<Link key={key} to={`/${key}`} className='menu-item' onClick={() => document.activeElement?.blur()}>
						<span className='flex h-8 w-8 items-center justify-center rounded bg-panel text-brand'>
							<Icon size={16} />
						</span>
						{label}
					</Link>
				))}
			</div>
		</div>
	</div>
);

export default Navbar;
