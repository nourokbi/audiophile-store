import { Plus, Trash2 } from 'lucide-react';

// The rows of what comes in the box, a quantity and an item name each, the dialog holds the rows and checks them
const BoxContentsEditor = ({ rows, error, onRowChange, onAddRow, onRemoveRow }) => (
	<div role='group' aria-label='In the box' className='flex flex-col gap-3'>
		<div className='flex items-center justify-between'>
			<span className={`field-label ${error ? 'text-red-600' : ''}`}>In the box (optional)</span>
			{error && <span className='text-[12px] font-medium text-red-600'>{error}</span>}
		</div>

		{rows.length === 0 && <p className='text-[14px] text-muted'>Nothing listed yet.</p>}

		{rows.map((row, rowIndex) => (
			<div key={row.rowId} className='flex items-center gap-2'>
				<input
					type='number'
					min='1'
					step='1'
					value={row.quantity}
					onChange={(event) => onRowChange(row.rowId, 'quantity', event.target.value)}
					aria-label={`Quantity of item ${rowIndex + 1}`}
					className='input w-20 shrink-0 text-center'
				/>
				<input
					type='text'
					maxLength={100}
					value={row.item}
					onChange={(event) => onRowChange(row.rowId, 'item', event.target.value)}
					placeholder='Headphone unit'
					aria-label={`Name of item ${rowIndex + 1}`}
					className='input min-w-0 flex-1'
				/>
				<button
					type='button'
					onClick={() => onRemoveRow(row.rowId)}
					aria-label={`Remove item ${rowIndex + 1}`}
					className='shrink-0 rounded p-3 text-muted hover:bg-panel hover:text-red-600'
				>
					<Trash2 size={16} />
				</button>
			</div>
		))}

		<button type='button' onClick={onAddRow} className='btn-secondary h-10 self-start px-4'>
			<Plus size={16} /> Add an item
		</button>
	</div>
);

export default BoxContentsEditor;
