import React from 'react';
import { CreditCard, RefreshCw, Undo2 } from 'lucide-react';

import PriceSummary from '../shop/PriceSummary';
import { formatDate } from '../../lib/format';
import { PAYMENT_LABELS } from '../../lib/orderHelpers';

// Fawaterak's raw message spaced out to read, or as it came when it is not JSON
function readablePayload(rawPayload) {
	try {
		return JSON.stringify(JSON.parse(rawPayload), null, 2);
	} catch {
		return rawPayload;
	}
}

// The order's payment and totals, and for a card order the messages from Fawaterak and a button to ask it again
const PaymentPanel = ({ order, events, loadError, isSyncing, syncError, onSync, isRefunding, refundError, onRefund }) => {
	const isCardOrder = order.paymentMethod === 'Card';
	const needsRefund = order.status === 'Cancelled' && order.paymentStatus === 'Succeeded';
	const [refundReference, setRefundReference] = React.useState('');

	return (
		<section className='card flex flex-col gap-4 p-6'>
			<h2 className='flex items-center gap-2 text-[18px]'>
				<CreditCard size={18} className='text-brand' /> Payment
			</h2>
			<p className='text-[14px]'>
				{PAYMENT_LABELS[order.paymentMethod]}, <span className='font-bold'>{order.paymentStatus.toLowerCase()}</span>
			</p>
			{needsRefund && (
				<form
					onSubmit={(event) => {
						event.preventDefault();
						onRefund(refundReference.trim());
					}}
					className='flex flex-col gap-3 rounded bg-red-50 p-3 text-[13px] text-red-700 dark:bg-red-950 dark:text-red-300'
				>
					<p className='font-bold'>
						Refund needed: this order was cancelled after it was paid. Refund it in the Fawaterak dashboard, then mark it
						refunded so the customer gets an email.
					</p>
					<label className='flex flex-col gap-1'>
						Refund id from Fawaterak (optional)
						<input
							value={refundReference}
							onChange={(event) => setRefundReference(event.target.value)}
							maxLength={255}
							className='rounded border border-line bg-surface px-3 py-2 text-ink'
						/>
					</label>
					<button type='submit' disabled={isRefunding} className='btn-primary'>
						<Undo2 size={16} /> {isRefunding ? 'Saving...' : 'Mark refunded'}
					</button>
					{refundError && <p className='error-text'>{refundError}</p>}
				</form>
			)}
			{order.paymentStatus === 'Refunded' && (
				<p className='rounded bg-panel p-3 text-[13px]'>
					Refunded {formatDate(order.refundedAt)}
					{order.refundReference && <>, Fawaterak refund {order.refundReference}</>}
				</p>
			)}
			<PriceSummary subTotal={order.subTotal} shippingFee={order.shippingFee} total={order.totalAmount} />

			{isCardOrder && (
				<>
					<h3 className='text-[15px] font-bold'>Messages from Fawaterak</h3>
					{loadError && <p className='error-text'>{loadError}</p>}
					{!loadError && events.length === 0 && (
						<p className='text-[13px] text-muted'>None yet. If the customer paid and the order did not move, sync it.</p>
					)}
					<ul className='flex flex-col gap-2'>
						{events.map((event) => (
							<li key={event.externalId} className='rounded bg-panel p-3 text-[13px]'>
								<details>
									<summary className='cursor-pointer'>
										<span className='font-bold'>{event.eventType}</span>{' '}
										<span className='text-muted'>{formatDate(event.receivedAt)}</span>
									</summary>
									<pre className='mt-2 max-h-48 overflow-auto whitespace-pre-wrap break-all text-[12px]'>
										{readablePayload(event.rawPayload)}
									</pre>
								</details>
							</li>
						))}
					</ul>
					<button onClick={onSync} disabled={isSyncing} className='btn-secondary w-full'>
						<RefreshCw size={16} className={isSyncing ? 'animate-spin' : ''} />
						{isSyncing ? 'Asking Fawaterak...' : 'Sync with gateway'}
					</button>
					{syncError && <p className='error-text'>{syncError}</p>}
				</>
			)}
		</section>
	);
};

export default PaymentPanel;
