import { createPortal } from 'react-dom';
import { X } from 'lucide-react';

// A small centered window over a dark backdrop, closed by the X, the backdrop or the cancel button
const Dialog = ({ title, onClose, children }) =>
	createPortal(
		<div className='fixed inset-0 z-[80] flex items-center justify-center bg-black/50 p-6' onClick={onClose}>
			<div
				role='dialog'
				aria-modal='true'
				aria-label={title}
				className='card w-full max-w-[420px] p-6 shadow-2xl'
				onClick={(e) => e.stopPropagation()}
			>
				<div className='mb-5 flex items-center justify-between'>
					<h2 className='text-[18px]'>{title}</h2>
					<button aria-label='Close' onClick={onClose} className='rounded p-2 hover:bg-panel'>
						<X size={18} />
					</button>
				</div>
				{children}
			</div>
		</div>,
		document.body,
	);

export default Dialog;
