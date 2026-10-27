import { Check, CircleCheck, Home, Package, Truck } from 'lucide-react';

const STEPS = [
	{ status: 'Pending', label: 'Placed', icon: Check },
	{ status: 'Confirmed', label: 'Confirmed', icon: CircleCheck },
	{ status: 'Processing', label: 'Preparing', icon: Package },
	{ status: 'Shipped', label: 'Shipped', icon: Truck },
	{ status: 'Delivered', label: 'Delivered', icon: Home },
];

// The step tracker from the orders design, done steps are orange and the line fills up to the current one
const OrderProgress = ({ status }) => {
	const currentStepIndex = STEPS.findIndex((step) => step.status === status);

	return (
		<ol className='grid grid-cols-5 rounded bg-panel px-2 py-5'>
			{STEPS.map(({ status: stepStatus, label, icon: Icon }, stepIndex) => {
				const isDone = stepIndex <= currentStepIndex;
				return (
					<li key={stepStatus} className='relative flex flex-col items-center gap-2 text-center'>
						{stepIndex > 0 && (
							<span className={`absolute right-1/2 top-4 h-0.5 w-full ${isDone ? 'bg-brand' : 'bg-panel-strong'}`} />
						)}
						<span
							className={`relative z-10 flex h-8 w-8 items-center justify-center rounded-full ${
								isDone ? 'bg-brand text-white' : 'bg-surface text-muted ring-1 ring-line'
							}`}
						>
							<Icon size={16} />
						</span>
						<span
							className={`text-[11px] font-bold uppercase tracking-wider ${isDone ? 'text-ink' : 'text-muted'} ${
								stepIndex === currentStepIndex ? '' : 'hidden md:block'
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

export default OrderProgress;
