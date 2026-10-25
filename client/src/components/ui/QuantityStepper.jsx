import { Minus, Plus } from 'lucide-react';

// The grey minus and plus box, it never goes below 1 or above max
const QuantityStepper = ({ value, onChange, max = 100, label = 'product', small = false }) => {
	const size = small ? 'h-9' : 'h-12';
	const button =
		'flex w-9 h-full items-center justify-center text-muted hover:text-ink disabled:opacity-30 disabled:hover:text-muted';

	return (
		<div className={`flex shrink-0 items-center overflow-hidden rounded bg-panel ${size}`}>
			<button
				type='button'
				aria-label={`One less ${label}`}
				disabled={value <= 1}
				onClick={() => onChange(Math.max(1, value - 1))}
				className={button}
			>
				<Minus size={14} />
			</button>
			<span className='min-w-[24px] text-center text-[15px] font-bold tabular-nums' aria-live='polite'>
				{value}
			</span>
			<button
				type='button'
				aria-label={`One more ${label}`}
				disabled={value >= max}
				onClick={() => onChange(Math.min(max, value + 1))}
				className={button}
			>
				<Plus size={14} />
			</button>
		</div>
	);
};

export default QuantityStepper;
