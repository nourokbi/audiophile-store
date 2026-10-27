import React from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, MapPin, User } from 'lucide-react';

import AdminPageHeader from '../../components/admin/AdminPageHeader';
import OrderStatusControl from '../../components/admin/OrderStatusControl';
import PaymentPanel from '../../components/admin/PaymentPanel';
import OrderItems from '../../components/orders/OrderItems';
import OrderProgress from '../../components/orders/OrderProgress';
import StatusBadge from '../../components/orders/StatusBadge';
import ConfirmDialog from '../../components/ui/ConfirmDialog';
import { useAdminOrder } from '../../hooks/useAdminOrder';
import { usePaymentPanel } from '../../hooks/usePaymentPanel';
import { formatDate, shortId } from '../../lib/format';

// One order for the admin: where it is, who it goes to, what is in it, its payment, and the buttons that move it on
const OrderDetail = () => {
	const { id } = useParams();
	const { order, loadError, changeStatus, reload } = useAdminOrder(id);
	const payment = usePaymentPanel(id, order?.paymentMethod === 'Card', reload);
	const [isSaving, setIsSaving] = React.useState(false);
	const [saveError, setSaveError] = React.useState('');
	const [isCancelDialogOpen, setIsCancelDialogOpen] = React.useState(false);

	const moveTo = async (nextStatus) => {
		setIsSaving(true);
		setSaveError('');
		try {
			await changeStatus(nextStatus);
			setIsCancelDialogOpen(false);
		} catch (error) {
			setSaveError(error.message);
		} finally {
			setIsSaving(false);
		}
	};

	const openCancelDialog = () => {
		setSaveError('');
		setIsCancelDialogOpen(true);
	};

	if (loadError || !order) {
		return (
			<div className='flex flex-col gap-6'>
				<BackLink />
				<p className={loadError ? 'error-text' : 'text-muted'}>{loadError || 'Loading the order...'}</p>
			</div>
		);
	}

	const isPaidByCard = order.paymentMethod === 'Card' && order.paymentStatus === 'Succeeded';

	return (
		<div className='flex flex-col gap-6'>
			<BackLink />
			<AdminPageHeader title={`Order #${shortId(order.id)}`}>
				<StatusBadge status={order.status} />
			</AdminPageHeader>
			<p className='-mt-3 text-[14px] text-muted'>
				Placed {formatDate(order.createdAt)}, last change {formatDate(order.updatedAt)}
			</p>

			<div className='grid grid-cols-1 items-start gap-6 xl:grid-cols-3'>
				<div className='flex flex-col gap-6 xl:col-span-2'>
					{order.status !== 'Cancelled' && (
						<section className='card p-6'>
							<OrderProgress status={order.status} />
						</section>
					)}

					<section className='card flex flex-col gap-4 p-6'>
						<h2 className='text-[18px]'>Items</h2>
						<OrderItems items={order.items} />
					</section>

					<div className='grid grid-cols-1 gap-6 md:grid-cols-2'>
						<InfoCard icon={User} title='Customer'>
							<p className='font-bold'>{order.customerName}</p>
							<a href={`mailto:${order.customerEmail}`} className='text-brand-dark hover:underline'>
								{order.customerEmail}
							</a>
						</InfoCard>
						<InfoCard icon={MapPin} title='Ship to'>
							<p className='font-bold'>{order.shipping.fullName}</p>
							<p className='text-muted'>
								{order.shipping.street}, {order.shipping.city}, {order.shipping.country}
							</p>
							<p className='text-muted'>{order.shipping.phone}</p>
						</InfoCard>
					</div>
				</div>

				<aside className='flex flex-col gap-6'>
					<OrderStatusControl
						nextStatuses={order.nextStatuses}
						isSaving={isSaving && !isCancelDialogOpen}
						errorMessage={isCancelDialogOpen ? '' : saveError}
						onMoveForward={moveTo}
						onCancelClick={openCancelDialog}
					/>

					<PaymentPanel
						order={order}
						events={payment.events}
						loadError={payment.loadError}
						isSyncing={payment.isSyncing}
						syncError={payment.syncError}
						onSync={payment.syncWithGateway}
					/>
				</aside>
			</div>

			{isCancelDialogOpen && (
				<ConfirmDialog
					title='Cancel this order?'
					message={
						isPaidByCard
							? 'This card order is paid, so cancelling means refunding it on Fawaterak by hand. Its items go back into stock.'
							: 'Its items go back into stock and a pending payment is marked failed.'
					}
					confirmLabel='Cancel order'
					isDangerous
					isWorking={isSaving}
					errorMessage={saveError}
					onConfirm={() => moveTo('Cancelled')}
					onClose={() => setIsCancelDialogOpen(false)}
				/>
			)}
		</div>
	);
};

const BackLink = () => (
	<Link to='/admin/orders' className='label flex items-center gap-2 text-muted hover:text-brand-dark'>
		<ArrowLeft size={16} /> All orders
	</Link>
);

const InfoCard = ({ icon: Icon, title, children }) => (
	<section className='card flex flex-col gap-2 p-6 text-[14px]'>
		<h2 className='mb-2 flex items-center gap-2 text-[18px]'>
			<Icon size={18} className='text-brand' /> {title}
		</h2>
		{children}
	</section>
);

export default OrderDetail;
