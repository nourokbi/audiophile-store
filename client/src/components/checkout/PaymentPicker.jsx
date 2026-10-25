import { Banknote, CreditCard, ShieldCheck } from 'lucide-react';

import { PAYMENT_LABELS } from '../../lib/orderHelpers';

const PAYMENT_METHODS = [
	{ value: 'Card', note: 'Paid on Fawaterak', icon: CreditCard },
	{ value: 'CashOnDelivery', note: 'Pay the courier', icon: Banknote },
];

// Card or cash on delivery, with a note on what happens next
const PaymentPicker = ({ paymentMethod, onSelect }) => (
	<>
		<div className='grid grid-cols-2 gap-3'>
			{PAYMENT_METHODS.map(({ value, note, icon: Icon }) => (
				<button
					key={value}
					type='button'
					onClick={() => onSelect(value)}
					className={`flex flex-col items-center gap-1 rounded p-4 transition-colors ${
						paymentMethod === value ? 'bg-brand-soft text-brand-dark ring-1 ring-brand' : 'bg-panel hover:bg-panel-strong'
					}`}
				>
					<Icon size={22} />
					<span className='label'>{PAYMENT_LABELS[value]}</span>
					<span className='text-[12px] text-muted'>{note}</span>
				</button>
			))}
		</div>
		<p className='flex gap-3 rounded bg-panel p-4 text-[14px] text-muted'>
			<ShieldCheck size={20} className='shrink-0 text-brand' />
			{paymentMethod === 'Card'
				? 'You type your card on Fawaterak’s secure page, never on ours. Staging test card 5123 4500 0000 0008, expiry 12/26, CVV 100.'
				: 'Pay in cash when the courier arrives. Make sure your address and phone are right.'}
		</p>
	</>
);

export default PaymentPicker;
