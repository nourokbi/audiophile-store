import Dialog from './Dialog';

// Asks before an action that cannot be undone, the page runs the action and passes its error back
const ConfirmDialog = ({ title, message, confirmLabel, isWorking, errorMessage, isDangerous = false, onConfirm, onClose }) => (
	<Dialog title={title} onClose={onClose}>
		<div className='flex flex-col gap-4'>
			<p className='text-[15px] text-muted'>{message}</p>
			{errorMessage && <p className='error-text'>{errorMessage}</p>}
			<div className='flex justify-end gap-3'>
				<button type='button' onClick={onClose} className='btn-secondary'>
					Keep it
				</button>
				<button
					type='button'
					onClick={onConfirm}
					disabled={isWorking}
					className={isDangerous ? 'btn bg-red-600 text-white hover:bg-red-700' : 'btn-primary'}
				>
					{isWorking ? 'Saving...' : confirmLabel}
				</button>
			</div>
		</div>
	</Dialog>
);

export default ConfirmDialog;
