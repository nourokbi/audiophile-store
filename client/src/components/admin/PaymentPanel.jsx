import { CreditCard, RefreshCw } from 'lucide-react';

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
const PaymentPanel = ({ order, events, loadError, isSyncing, syncError, onSync }) => {
	const isCardOrder = order.paymentMethod === 'Card';
	const needsRefund = order.status === 'Cancelled' && order.paymentStatus === 'Succeeded';

	return (
		<section className='card flex flex-col gap-4 p-6'>
			<h2 className='flex items-center gap-2 text-[18px]'>
				<CreditCard size={18} className='text-brand' /> Payment
			</h2>
			<p className='text-[14px]'>
				{PAYMENT_LABELS[order.paymentMethod]}, <span className='font-bold'>{order.paymentStatus.toLowerCase()}</span>
			</p>
			{needsRefund && (
				<p className='rounded bg-red-50 p-3 text-[13px] font-bold text-red-700 dark:bg-red-950 dark:text-red-300'>
					Refund needed: this order was cancelled after it was paid, refund it in the Fawaterak dashboard.
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
