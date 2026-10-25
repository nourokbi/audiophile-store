import { ChevronDown } from 'lucide-react';

// A dropdown in the shop's input style, hideLabel keeps the label for screen readers only
const Select = ({ label, hideLabel = false, className = '', children, ...selectProps }) => (
	<label className={`flex flex-col gap-2 ${className}`}>
		<span className={hideLabel ? 'sr-only' : 'field-label'}>{label}</span>
		<span className='relative flex items-center'>
			<select className='input cursor-pointer appearance-none pr-10' {...selectProps}>
				{children}
			</select>
			<ChevronDown size={18} className='pointer-events-none absolute right-4 text-muted' />
		</span>
	</label>
);

export default Select;
