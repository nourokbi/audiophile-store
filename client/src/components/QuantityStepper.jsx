// The grey "- 1 +" box from the product page design. Never goes below 1 or above max.
const QuantityStepper = ({ value, onChange, max = 100, label = 'product', className = '' }) => {
	return (
		<div className={`w-[120px] h-[48px] flex justify-between bg-gray-light px-[15px] items-center shrink-0 ${className}`}>
			<button
				type='button'
				aria-label={`One less ${label}`}
				disabled={value <= 1}
				onClick={() => onChange(Math.max(1, value - 1))}
				className='text-[14px] text-black/30 hover:text-cream font-bold tracking-[1px] disabled:hover:text-black/30 disabled:cursor-not-allowed px-1'
			>
				-
			</button>
			<span className='text-[14px] text-black font-bold tracking-[1px] tabular-nums' aria-live='polite'>
				{value}
			</span>
			<button
				type='button'
				aria-label={`One more ${label}`}
				disabled={value >= max}
				onClick={() => onChange(Math.min(max, value + 1))}
				className='text-[14px] text-black/30 hover:text-cream font-bold tracking-[1px] disabled:hover:text-black/30 disabled:cursor-not-allowed px-1'
			>
				+
			</button>
		</div>
	);
};

export default QuantityStepper;
