import { Check, CircleCheck, Home, Package, Truck } from 'lucide-react';

import { formatPrice } from '../lib/format';

const BADGE = {
	Pending: 'bg-panel text-muted',
	Confirmed: 'bg-brand-soft text-brand-dark',
	Processing: 'bg-brand-soft text-brand-dark',
	Shipped: 'bg-ink text-white',
	Delivered: 'bg-emerald-50 text-emerald-700',
	Cancelled: 'bg-red-50 text-red-700',
};

export const StatusBadge = ({ status }) => (
	<span className={`rounded-full px-3 py-1 text-[11px] font-bold uppercase tracking-widest ${BADGE[status] ?? ''}`}>
		{status}
	</span>
);

const STEPS = [
	{ status: 'Pending', label: 'Placed', icon: Check },
	{ status: 'Confirmed', label: 'Confirmed', icon: CircleCheck },
	{ status: 'Processing', label: 'Preparing', icon: Package },
	{ status: 'Shipped', label: 'Shipped', icon: Truck },
	{ status: 'Delivered', label: 'Delivered', icon: Home },
];

// The step tracker from the orders design, done steps are orange and the line fills up to the current one
export const OrderProgress = ({ status }) => {
	const current = STEPS.findIndex((s) => s.status === status);

	return (
		<ol className='grid grid-cols-5 rounded bg-panel px-2 py-5'>
			{STEPS.map(({ status: step, label, icon: Icon }, i) => {
				const done = i <= current;
				return (
					<li key={step} className='relative flex flex-col items-center gap-2 text-center'>
						{i > 0 && (
							<span className={`absolute right-1/2 top-4 h-0.5 w-full ${i <= current ? 'bg-brand' : 'bg-panel-strong'}`} />
						)}
						<span
							className={`relative flex h-8 w-8 items-center justify-center rounded-full ${
								done ? 'bg-brand text-white' : 'bg-white text-muted ring-1 ring-line'
							}`}
						>
							<Icon size={16} />
						</span>
						<span
							className={`text-[11px] font-bold uppercase tracking-wider ${done ? 'text-ink' : 'text-muted'} ${
								i === current ? '' : 'hidden md:block'
							}`}
						>
							{label}
						</span>
					</li>
				);
			})}
		</ol>
	);
};

// The products of an order as small rows with picture, name, quantity and line total
export const OrderItems = ({ items }) => (
	<ul className='flex flex-col gap-3'>
		{items.map((item) => (
			<li key={item.productId} className='flex items-center justify-between gap-4 rounded bg-panel p-3'>
				<div className='flex min-w-0 items-center gap-4'>
					{item.productImageUrl && <img src={item.productImageUrl} alt='' className='h-14 w-14 rounded bg-white' />}
					<div className='min-w-0'>
						<p className='truncate text-[15px] font-bold'>{item.productName}</p>
						<p className='text-[13px] text-muted'>
							{formatPrice(item.unitPrice)} x {item.quantity}
						</p>
					</div>
				</div>
				<p className='shrink-0 text-[16px] font-bold'>{formatPrice(item.lineTotal)}</p>
			</li>
		))}
	</ul>
);
