import { ChevronLeft, ChevronRight } from 'lucide-react';

const SQUARE = 'flex h-11 w-11 items-center justify-center rounded shadow-sm transition-colors disabled:opacity-40';

// The first page, the last page and one on each side of the current one, with a gap marker where pages are skipped
function visiblePages(currentPage, totalPages) {
	const pageNumbers = [...new Set([1, currentPage - 1, currentPage, currentPage + 1, totalPages])]
		.filter((pageNumber) => pageNumber >= 1 && pageNumber <= totalPages)
		.sort((a, b) => a - b);
	return pageNumbers.flatMap((pageNumber, index) =>
		index > 0 && pageNumber - pageNumbers[index - 1] > 1 ? [`gap-before-${pageNumber}`, pageNumber] : [pageNumber],
	);
}

// Previous, the page numbers and next, the current page is orange
const Pagination = ({ currentPage, totalPages, onPageChange }) => (
	<nav aria-label='Pages' className='flex items-center justify-center gap-2 pt-4'>
		<button
			aria-label='Previous page'
			disabled={currentPage === 1}
			onClick={() => onPageChange(currentPage - 1)}
			className={`${SQUARE} bg-surface hover:bg-panel`}
		>
			<ChevronLeft size={18} />
		</button>
		{visiblePages(currentPage, totalPages).map((pageNumber) =>
			typeof pageNumber === 'string' ? (
				<span key={pageNumber} className='w-6 text-center text-muted'>
					...
				</span>
			) : (
				<button
					key={pageNumber}
					aria-current={pageNumber === currentPage ? 'page' : undefined}
					onClick={() => onPageChange(pageNumber)}
					className={`${SQUARE} label ${pageNumber === currentPage ? 'bg-brand text-white' : 'bg-surface hover:bg-panel'}`}
				>
					{pageNumber}
				</button>
			),
		)}
		<button
			aria-label='Next page'
			disabled={currentPage === totalPages}
			onClick={() => onPageChange(currentPage + 1)}
			className={`${SQUARE} bg-surface hover:bg-panel`}
		>
			<ChevronRight size={18} />
		</button>
	</nav>
);

export default Pagination;
