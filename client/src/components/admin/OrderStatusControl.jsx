import { ArrowRight, XCircle } from 'lucide-react';

import { STATUS_ACTIONS } from '../../lib/orderHelpers';

// One button per status the API allows next, cancelling is red and the page asks before it runs
const OrderStatusControl = ({ nextStatuses, isSaving, errorMessage, onMoveForward, onCancelClick }) => {
	const forwardStatuses = nextStatuses.filter((status) => status !== 'Cancelled');
	const canCancel = nextStatuses.includes('Cancelled');

	return (
		<section className='card flex flex-col gap-4 p-6'>
			<h2 className='text-[18px]'>Next step</h2>
			{nextStatuses.length === 0 && <p className='text-[14px] text-muted'>This order is finished, nothing more can change.</p>}
			{forwardStatuses.map((status) => (
				<button key={status} onClick={() => onMoveForward(status)} disabled={isSaving} className='btn-primary w-full'>
					{isSaving ? 'Saving...' : STATUS_ACTIONS[status]} <ArrowRight size={16} />
				</button>
			))}
			{canCancel && (
				<button
					onClick={onCancelClick}
					disabled={isSaving}
					className='btn w-full bg-red-50 text-red-700 hover:bg-red-100 dark:bg-red-950 dark:text-red-300 dark:hover:bg-red-900'
				>
					<XCircle size={16} /> {STATUS_ACTIONS.Cancelled}
				</button>
			)}
			{errorMessage && <p className='error-text'>{errorMessage}</p>}
		</section>
	);
};

export default OrderStatusControl;
