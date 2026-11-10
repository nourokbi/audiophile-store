import { AlertTriangle, Bell, CheckCheck, PackageX, ShoppingBag, Undo2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

import AdminPageHeader from '../../components/admin/AdminPageHeader';
import EmptyState from '../../components/ui/EmptyState';
import { useAdminNotifications } from '../../hooks/useAdminNotifications';
import { formatDate } from '../../lib/format';

const KIND_ICONS = { NewOrder: ShoppingBag, OrderCancelled: PackageX, RefundNeeded: Undo2, LowStock: AlertTriangle };

const LOW_STOCK_PAGE = '/admin/products?availability=OnSale&lowStockOnly=true&sort=StockLowToHigh';

// Where a notification leads: its order, or the low stock products
const linkFor = (notification) =>
	notification.orderId ? `/admin/orders/${notification.orderId}` : notification.productId ? LOW_STOCK_PAGE : null;

// New orders, cancellations, refunds to send and low stock, opening one marks it read
const Notifications = () => {
	const bell = useAdminNotifications();
	const navigate = useNavigate();

	const open = (notification) => {
		if (!notification.isRead) bell.markRead(notification.id);
		const link = linkFor(notification);
		if (link) navigate(link);
	};

	return (
		<div className='flex flex-col gap-6'>
			<AdminPageHeader title='Notifications'>
				<button onClick={bell.markAllRead} disabled={bell.unreadCount === 0 || bell.isMarkingAll} className='btn-secondary'>
					<CheckCheck size={16} /> Mark all read
				</button>
			</AdminPageHeader>

			{bell.isError && <p className='error-text'>The notifications could not be loaded.</p>}
			{bell.isPending && <p className='text-muted'>Loading...</p>}
			{!bell.isPending && bell.notifications.length === 0 && (
				<EmptyState icon={Bell} title='Nothing new' text='New orders, cancellations and low stock will show up here.' />
			)}

			<ul className='flex flex-col gap-3'>
				{bell.notifications.map((notification) => {
					const Icon = KIND_ICONS[notification.kind] ?? Bell;
					return (
						<li key={notification.id}>
							<button
								onClick={() => open(notification)}
								className={`card flex w-full items-start gap-4 p-5 text-left transition-colors hover:bg-panel ${
									notification.isRead ? 'opacity-70' : 'border-l-4 border-l-brand'
								}`}
							>
								<Icon size={20} className={`mt-1 shrink-0 ${notification.isRead ? 'text-muted' : 'text-brand'}`} />
								<span className='flex min-w-0 flex-1 flex-col gap-1'>
									<span className='flex flex-wrap items-baseline justify-between gap-2'>
										<strong className='break-words'>{notification.title}</strong>
										<span className='text-sm text-muted'>{formatDate(notification.createdAt)}</span>
									</span>
									<span className='text-muted'>{notification.message}</span>
								</span>
							</button>
						</li>
					);
				})}
			</ul>
		</div>
	);
};

export default Notifications;
