import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { LayoutDashboard, LogIn, LogOut, Moon, Package, Settings, Sun, User, UserPlus } from 'lucide-react';

import { SwitchTrack } from '../ui/Switch';
import { useAuth } from '../../context/AuthContext';
import { useTheme } from '../../context/ThemeContext';

// The round account button and the small menu under it, a click outside or Escape closes it
const AccountMenu = () => {
	const { user, isAdmin, logout } = useAuth();
	const { theme, toggleTheme } = useTheme();
	const navigate = useNavigate();
	const [isOpen, setIsOpen] = React.useState(false);
	const menuRef = React.useRef(null);

	React.useEffect(() => {
		if (!isOpen) return;
		const closeOnOutsideClick = (event) => {
			if (!menuRef.current?.contains(event.target)) setIsOpen(false);
		};
		const closeOnEscape = (event) => {
			if (event.key === 'Escape') setIsOpen(false);
		};
		document.addEventListener('mousedown', closeOnOutsideClick);
		document.addEventListener('keydown', closeOnEscape);
		return () => {
			document.removeEventListener('mousedown', closeOnOutsideClick);
			document.removeEventListener('keydown', closeOnEscape);
		};
	}, [isOpen]);

	const closeMenu = () => setIsOpen(false);

	const logOutAndGoHome = async () => {
		closeMenu();
		await logout();
		navigate('/');
	};

	return (
		<div ref={menuRef} className='relative'>
			<button
				aria-label='Account'
				aria-haspopup='menu'
				aria-expanded={isOpen}
				onClick={() => setIsOpen((wasOpen) => !wasOpen)}
				className='flex h-10 w-10 items-center justify-center rounded-full bg-brand text-white transition-colors hover:bg-brand-hover'
			>
				{user ? <span className='text-[15px] font-bold'>{user.name.charAt(0).toUpperCase()}</span> : <User size={18} />}
			</button>

			{isOpen && (
				<div role='menu' className='card absolute right-0 top-full z-50 mt-3 flex w-64 flex-col p-2 shadow-xl'>
					{user && (
						<div className='mb-1 border-b border-line px-3 pb-3 pt-2'>
							<p className='truncate text-[15px] font-bold'>{user.name}</p>
							<p className='truncate text-[13px] text-muted'>{user.email}</p>
							<p className='mt-1 text-[10px] font-bold uppercase tracking-widest text-brand-dark'>
								{isAdmin ? 'Admin' : 'Customer'}
							</p>
						</div>
					)}

					{user ? (
						<>
							<MenuLink to='/orders' icon={Package} onClick={closeMenu}>
								My orders
							</MenuLink>
							{isAdmin && (
								<MenuLink to='/admin' icon={LayoutDashboard} onClick={closeMenu}>
									Admin dashboard
								</MenuLink>
							)}
							<MenuLink to='/settings' icon={Settings} onClick={closeMenu}>
								Settings
							</MenuLink>
						</>
					) : (
						<>
							<MenuLink to='/login' icon={LogIn} onClick={closeMenu}>
								Log in
							</MenuLink>
							<MenuLink to='/register' icon={UserPlus} onClick={closeMenu}>
								Create account
							</MenuLink>
						</>
					)}

					<button
						role='menuitemcheckbox'
						aria-checked={theme === 'dark'}
						onClick={toggleTheme}
						className='menu-item justify-between py-2'
					>
						<span className='flex items-center gap-3'>
							{theme === 'dark' ? <Moon size={16} /> : <Sun size={16} />}
							Dark theme
						</span>
						<SwitchTrack isOn={theme === 'dark'} />
					</button>

					{user && (
						<div className='mt-1 border-t border-line pt-1'>
							<button
								role='menuitem'
								onClick={logOutAndGoHome}
								className='menu-item py-2 text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-950'
							>
								<LogOut size={16} /> Log out
							</button>
						</div>
					)}
				</div>
			)}
		</div>
	);
};

const MenuLink = ({ to, icon: Icon, onClick, children }) => (
	<Link role='menuitem' to={to} onClick={onClick} className='menu-item py-2'>
		<Icon size={16} /> {children}
	</Link>
);

export default AccountMenu;
