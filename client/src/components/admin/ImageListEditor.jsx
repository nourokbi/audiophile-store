import { Plus, Star, Trash2 } from 'lucide-react';

// The product's photo addresses, one row each with a preview, the first row is the cover the shop shows on the cards
const ImageListEditor = ({ rows, maxRows, error, isValidAddress, onRowChange, onAddRow, onRemoveRow, onMakeCover }) => (
	<div role='group' aria-label='Images' className='flex flex-col gap-3'>
		<div className='flex items-center justify-between'>
			<span className={`field-label ${error ? 'text-red-600' : ''}`}>Images (optional)</span>
			{error && <span className='text-[12px] font-medium text-red-600'>{error}</span>}
		</div>

		{rows.length === 0 && <p className='text-[14px] text-muted'>No photos yet, the shop shows a grey square.</p>}

		{rows.map((row, rowIndex) => {
			const address = row.url.trim();
			return (
				<div key={row.rowId} className='flex items-center gap-2'>
					<div className='h-12 w-12 shrink-0 overflow-hidden rounded bg-panel'>
						{address !== '' && isValidAddress(address) && (
							<img
								src={address}
								alt=''
								className='h-full w-full object-cover'
								onError={(event) => (event.currentTarget.style.visibility = 'hidden')}
								onLoad={(event) => (event.currentTarget.style.visibility = 'visible')}
							/>
						)}
					</div>
					<input
						type='text'
						inputMode='url'
						value={row.url}
						onChange={(event) => onRowChange(row.rowId, event.target.value)}
						placeholder='/assets/products/name.jpg or https://...'
						aria-label={`Address of photo ${rowIndex + 1}`}
						className='input min-w-0 flex-1'
					/>
					{rowIndex === 0 ? (
						<span className='w-11 shrink-0 text-center text-[11px] font-bold uppercase text-brand-dark'>Cover</span>
					) : (
						<button
							type='button'
							onClick={() => onMakeCover(row.rowId)}
							aria-label={`Make photo ${rowIndex + 1} the cover`}
							title='Make this the cover'
							className='w-11 shrink-0 rounded p-3 text-muted hover:bg-panel hover:text-brand-dark'
						>
							<Star size={16} />
						</button>
					)}
					<button
						type='button'
						onClick={() => onRemoveRow(row.rowId)}
						aria-label={`Remove photo ${rowIndex + 1}`}
						className='shrink-0 rounded p-3 text-muted hover:bg-panel hover:text-red-600'
					>
						<Trash2 size={16} />
					</button>
				</div>
			);
		})}

		{rows.length < maxRows && (
			<button type='button' onClick={onAddRow} className='btn-secondary h-10 self-start px-4'>
				<Plus size={16} /> Add a photo
			</button>
		)}
	</div>
);

export default ImageListEditor;
