import { RotateCcw, Search } from 'lucide-react';

// The search box and the date range above the admin orders table
const OrderFilters = ({ searchText, onSearchTextChange, filters, onFilterChange, hasFilters, onClearFilters }) => (
	<div className='card flex flex-col gap-4 p-6'>
		<div className='grid grid-cols-1 gap-3 md:grid-cols-[1fr_auto_auto]'>
			<div className='relative flex items-center'>
				<Search size={18} className='pointer-events-none absolute left-4 text-muted' />
				<input
					type='search'
					value={searchText}
					onChange={(event) => onSearchTextChange(event.target.value)}
					placeholder='Customer name, email or order number'
					aria-label='Search orders'
					className='input pl-11'
				/>
			</div>
			<DateField
				label='From'
				value={filters.from}
				max={filters.to || undefined}
				onChange={(value) => onFilterChange('from', value)}
			/>
			<DateField
				label='To'
				value={filters.to}
				min={filters.from || undefined}
				onChange={(value) => onFilterChange('to', value)}
			/>
		</div>
		{hasFilters && (
			<button onClick={onClearFilters} className='label flex items-center gap-2 self-end text-muted hover:text-brand-dark'>
				<RotateCcw size={14} /> Clear filters
			</button>
		)}
	</div>
);

const DateField = ({ label, value, min, max, onChange }) => (
	<label className='flex items-center gap-3'>
		<span className='field-label text-muted'>{label}</span>
		<input
			type='date'
			value={value}
			min={min}
			max={max}
			onChange={(event) => onChange(event.target.value)}
			className='input w-auto cursor-pointer'
		/>
	</label>
);

export default OrderFilters;
