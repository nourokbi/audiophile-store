import { RotateCcw, Search } from 'lucide-react';

import Select from '../ui/Select';
import Switch from '../ui/Switch';
import { PRICE_RANGES, SORT_ORDERS } from '../../hooks/useShopFilters';
import { CATEGORIES } from '../../lib/navigation';

const CATEGORY_CHIPS = [{ key: 'all', label: 'All items' }, ...CATEGORIES];

// The category chips, the search box, the price and sort dropdowns and the stock switch above the product grid
const ShopFilters = ({ categoryKey, onCategoryClick, filters, onFilterChange, hasFilters, onClearFilters }) => (
	<div className='card flex flex-col gap-5 p-6'>
		<div className='flex flex-wrap gap-2'>
			{CATEGORY_CHIPS.map(({ key, label }) => (
				<button
					key={key}
					onClick={() => onCategoryClick(key)}
					className={`chip ${categoryKey === key ? 'bg-brand text-white shadow-sm' : 'bg-panel hover:bg-panel-strong'}`}
				>
					{label}
				</button>
			))}
		</div>

		<div className='grid grid-cols-1 gap-3 md:grid-cols-12'>
			<div className='relative flex items-center md:col-span-6'>
				<Search size={18} className='pointer-events-none absolute left-4 text-muted' />
				<input
					type='search'
					value={filters.searchText}
					onChange={(event) => onFilterChange('searchText', event.target.value)}
					placeholder='Search products'
					aria-label='Search products'
					className='input pl-11'
				/>
			</div>
			<Select
				label='Price'
				hideLabel
				value={filters.priceRange}
				onChange={(event) => onFilterChange('priceRange', event.target.value)}
				className='md:col-span-3'
			>
				{PRICE_RANGES.map((range) => (
					<option key={range.key} value={range.key}>
						{range.label}
					</option>
				))}
			</Select>
			<Select
				label='Sort'
				hideLabel
				value={filters.sortOrder}
				onChange={(event) => onFilterChange('sortOrder', event.target.value)}
				className='md:col-span-3'
			>
				{Object.entries(SORT_ORDERS).map(([key, sortOrder]) => (
					<option key={key} value={key}>
						{sortOrder.label}
					</option>
				))}
			</Select>
		</div>

		<div className='flex flex-wrap items-center justify-between gap-4'>
			<Switch isOn={filters.inStockOnly} onClick={() => onFilterChange('inStockOnly', !filters.inStockOnly)}>
				<span className='label'>In stock only</span>
			</Switch>
			{hasFilters && (
				<button onClick={onClearFilters} className='label flex items-center gap-2 text-muted hover:text-brand-dark'>
					<RotateCcw size={14} /> Clear filters
				</button>
			)}
		</div>
	</div>
);

export default ShopFilters;
