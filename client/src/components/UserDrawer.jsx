import { createPortal } from 'react-dom';
import { Link, useNavigate } from 'react-router-dom';
import { LayoutDashboard, LogIn, LogOut, Moon, Package, Settings, Sun, UserPlus, X } from 'lucide-react';

import { useAuth } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';

// The panel on the right with the account links and the theme switch, rendered on body so the header does not clip it
const UserDrawer = ({ onClose }) => {
	const { user, logout } = useAuth();
	const { theme, toggleTheme } = useTheme();
	const navigate = useNavigate();
	const isAdmin = user?.roles?.includes('Admin');

	const onLogout = async () => {
		onClose();
		await logout();
		navigate('/');
	};

	return createPortal(
		<>
			<div className='fixed inset-0 z-[60] bg-black/40' onClick={onClose} />
			<aside className='fixed inset-y-0 right-0 z-[70] flex w-full max-w-[340px] flex-col bg-surface shadow-2xl'>
				<div className='flex items-center justify-between border-b border-line p-6'>
					{user ? (
						<div className='flex min-w-0 items-center gap-3'>
							<span className='flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-brand text-[18px] font-bold text-white'>
								{user.name.charAt(0).toUpperCase()}
							</span>
							<div className='min-w-0'>
								<p className='truncate text-[16px] font-bold'>{user.name}</p>
								<p className='truncate text-[13px] text-muted'>{user.email}</p>
								<p className='text-[10px] font-bold uppercase tracking-widest text-brand-dark'>
									{isAdmin ? 'Admin' : 'Customer'}
								</p>
							</div>
						</div>
					) : (
						<p className='text-[16px] font-bold'>Welcome</p>
					)}
					<button aria-label='Close' onClick={onClose} className='rounded p-2 hover:bg-panel'>
						<X size={20} />
					</button>
				</div>

				<nav className='flex flex-1 flex-col gap-1 p-4'>
					{user ? (
						<>
							<Link to='/orders' onClick={onClose} className='menu-item'>
								<Package size={18} /> My orders
							</Link>
							{isAdmin && (
								<Link to='/admin' onClick={onClose} className='menu-item'>
									<LayoutDashboard size={18} /> Admin dashboard
								</Link>
							)}
							<Link to='/settings' onClick={onClose} className='menu-item'>
								<Settings size={18} /> Settings
							</Link>
						</>
					) : (
						<>
							<Link to='/login' onClick={onClose} className='menu-item'>
								<LogIn size={18} /> Log in
							</Link>
							<Link to='/register' onClick={onClose} className='menu-item'>
								<UserPlus size={18} /> Create account
							</Link>
						</>
					)}

					<button onClick={toggleTheme} className='menu-item justify-between' role='switch' aria-checked={theme === 'dark'}>
						<span className='flex items-center gap-3'>
							{theme === 'dark' ? <Moon size={18} /> : <Sun size={18} />}
							Dark theme
						</span>
						<span className={`relative h-6 w-11 rounded-full transition-colors ${theme === 'dark' ? 'bg-brand' : 'bg-panel-strong'}`}>
							<span
								className={`absolute left-[2px] top-[2px] h-5 w-5 rounded-full bg-white transition-transform ${
									theme === 'dark' ? 'translate-x-5' : ''
								}`}
							/>
						</span>
					</button>
				</nav>

				{user && (
					<div className='border-t border-line p-4'>
						<button onClick={onLogout} className='menu-item text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-950'>
							<LogOut size={18} /> Log out
						</button>
					</div>
				)}
			</aside>
		</>,
		document.body,
	);
};

export default UserDrawer;
