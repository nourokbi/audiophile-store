import React from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { ClipboardList, LayoutDashboard, Package, PanelLeftClose, PanelLeftOpen, Wallet } from 'lucide-react';

const SECTIONS = [
	{ to: '/admin', label: 'Overview', icon: LayoutDashboard, end: true },
	{ to: '/admin/products', label: 'Products', icon: Package },
	{ to: '/admin/orders', label: 'Orders', icon: ClipboardList },
	{ to: '/admin/income', label: 'Income', icon: Wallet },
];

// Remembered in this browser only, it is a preference and not data
const COLLAPSED_KEY = 'audiophile.adminSidebarCollapsed';

function readIsCollapsed() {
	try {
		return localStorage.getItem(COLLAPSED_KEY) === 'true';
	} catch {
		return false;
	}
}

// The dashboard frame: a sticky menu on the left that folds into a bar of icons, and the admin page on the right
// On a phone the menu always stays folded so the page keeps its width
const AdminLayout = () => {
	const [isCollapsed, setIsCollapsed] = React.useState(readIsCollapsed);

	React.useEffect(() => {
		try {
			localStorage.setItem(COLLAPSED_KEY, String(isCollapsed));
		} catch {
			// Not remembered, but the menu still folds
		}
	}, [isCollapsed]);

	const labelClass = isCollapsed ? 'hidden' : 'hidden md:inline';

	return (
		<div className='flex items-start'>
			<aside
				className={`sticky top-20 flex h-[calc(100vh-5rem)] w-16 shrink-0 flex-col border-r border-line bg-surface transition-[width] duration-200 ${
					isCollapsed ? '' : 'md:w-60'
				}`}
			>
				<p className={`eyebrow px-5 pb-2 pt-6 ${labelClass}`}>Admin</p>
				<nav aria-label='Admin sections' className='flex flex-1 flex-col gap-1 p-2'>
					{SECTIONS.map(({ to, label, icon: Icon, end }) => (
						<NavLink
							key={to}
							to={to}
							end={end}
							title={label}
							aria-label={label}
							className={({ isActive }) =>
								`label flex h-11 items-center gap-3 rounded px-[14px] transition-colors ${
									isActive ? 'bg-brand-soft text-brand-dark' : 'text-muted hover:bg-panel hover:text-ink'
								}`
							}
						>
							<Icon size={20} className='shrink-0' />
							<span className={`truncate ${labelClass}`}>{label}</span>
						</NavLink>
					))}
				</nav>
				<button
					onClick={() => setIsCollapsed((wasCollapsed) => !wasCollapsed)}
					aria-label={isCollapsed ? 'Expand the menu' : 'Collapse the menu'}
					title={isCollapsed ? 'Expand the menu' : 'Collapse the menu'}
					className='label m-2 hidden h-11 items-center gap-3 rounded px-[14px] text-muted transition-colors hover:bg-panel hover:text-ink md:flex'
				>
					{isCollapsed ? <PanelLeftOpen size={20} className='shrink-0' /> : <PanelLeftClose size={20} className='shrink-0' />}
					<span className={labelClass}>Collapse</span>
				</button>
			</aside>

			<div className='min-w-0 flex-1 px-4 pb-24 pt-8 md:px-10'>
				<React.Suspense fallback={<p className='text-muted'>Loading...</p>}>
					<Outlet />
				</React.Suspense>
			</div>
		</div>
	);
};

export default AdminLayout;
