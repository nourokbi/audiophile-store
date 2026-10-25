import { ChevronLeft, ChevronRight } from 'lucide-react';

// Previous, every page number and next, the current page is orange
const Pagination = ({ current, pages, onChange }) => {
	const square = 'flex h-11 w-11 items-center justify-center rounded shadow-sm transition-colors disabled:opacity-40';
	return (
		<nav aria-label='Pages' className='flex items-center justify-center gap-2 pt-4'>
			<button aria-label='Previous page' disabled={current === 1} onClick={() => onChange(current - 1)} className={`${square} bg-surface hover:bg-panel`}>
				<ChevronLeft size={18} />
			</button>
			{Array.from({ length: pages }, (_, i) => i + 1).map((n) => (
				<button
					key={n}
					aria-current={n === current ? 'page' : undefined}
					onClick={() => onChange(n)}
					className={`${square} label ${n === current ? 'bg-brand text-white' : 'bg-surface hover:bg-panel'}`}
				>
					{n}
				</button>
			))}
			<button aria-label='Next page' disabled={current === pages} onClick={() => onChange(current + 1)} className={`${square} bg-surface hover:bg-panel`}>
				<ChevronRight size={18} />
			</button>
		</nav>
	);
};

export default Pagination;
