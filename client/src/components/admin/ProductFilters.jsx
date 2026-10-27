import { RotateCcw, Search } from 'lucide-react';

import Select from '../ui/Select';
import Switch from '../ui/Switch';
import { CATEGORIES } from '../../lib/navigation';

const AVAILABILITY_CHOICES = [
	{ value: 'All', label: 'Shown and hidden' },
	{ value: 'OnSale', label: 'On sale' },
	{ value: 'Hidden', label: 'Hidden' },
];

// The values are the API's ProductSort names
const SORT_CHOICES = [
	{ value: 'Name', label: 'Name, A to Z' },
	{ value: 'Newest', label: 'Newest first' },
	{ value: 'PriceLowToHigh', label: 'Price, low to high' },
	{ value: 'PriceHighToLow', label: 'Price, high to low' },
	{ value: 'StockLowToHigh', label: 'Stock, lowest first' },
];

// The search box and filters above the admin product table, each change goes straight to the page
const ProductFilters = ({
	searchText,
	onSearchTextChange,
	filters,
	onFilterChange,
	lowStockLimit,
	hasFilters,
	onClearFilters,
}) => (
	<div className='card flex flex-col gap-4 p-6'>
		<div className='grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4'>
			<div className='relative flex items-center md:col-span-2 xl:col-span-1'>
				<Search size={18} className='pointer-events-none absolute left-4 text-muted' />
				<input
					type='search'
					value={searchText}
					onChange={(event) => onSearchTextChange(event.target.value)}
					placeholder='Search name or description'
					aria-label='Search products'
					className='input pl-11'
				/>
			</div>
			<Select
				label='Category'
				hideLabel
				value={filters.category}
				onChange={(event) => onFilterChange('category', event.target.value)}
			>
				<option value=''>Every category</option>
				{CATEGORIES.map((category) => (
					<option key={category.key} value={category.key}>
						{category.label}
					</option>
				))}
			</Select>
			<Select
				label='Availability'
				hideLabel
				value={filters.availability}
				onChange={(event) => onFilterChange('availability', event.target.value)}
			>
				{AVAILABILITY_CHOICES.map((choice) => (
					<option key={choice.value} value={choice.value}>
						{choice.label}
					</option>
				))}
			</Select>
			<Select label='Sort' hideLabel value={filters.sort} onChange={(event) => onFilterChange('sort', event.target.value)}>
				{SORT_CHOICES.map((choice) => (
					<option key={choice.value} value={choice.value}>
						{choice.label}
					</option>
				))}
			</Select>
		</div>

		<div className='flex flex-wrap items-center justify-between gap-4'>
			<Switch isOn={filters.lowStockOnly} onClick={() => onFilterChange('lowStockOnly', !filters.lowStockOnly)}>
				<span className='label'>Low stock only, {lowStockLimit} or fewer</span>
			</Switch>
			{hasFilters && (
				<button onClick={onClearFilters} className='label flex items-center gap-2 text-muted hover:text-brand-dark'>
					<RotateCcw size={14} /> Clear filters
				</button>
			)}
		</div>
	</div>
);

export default ProductFilters;
