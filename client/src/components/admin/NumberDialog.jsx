import React from 'react';

import Dialog from '../ui/Dialog';

// Asks for one positive number and shows what the change will do before it is saved
const NumberDialog = ({ title, label, initialValue, minimum, isSaving, errorMessage, describeChange, onClose, onSave }) => {
	const [inputText, setInputText] = React.useState(String(initialValue));
	const enteredNumber = Number(inputText);
	const isValid = Number.isFinite(enteredNumber) && enteredNumber >= minimum;

	const submitForm = (event) => {
		event.preventDefault();
		if (isValid) onSave(enteredNumber);
	};

	return (
		<Dialog title={title} onClose={onClose}>
			<form className='flex flex-col gap-4' onSubmit={submitForm}>
				<label className='flex flex-col gap-2'>
					<span className='field-label'>{label}</span>
					<input
						type='number'
						min={minimum}
						autoFocus
						value={inputText}
						onChange={(event) => setInputText(event.target.value)}
						className='input'
					/>
				</label>
				<p className='text-[14px] text-muted'>
					{isValid ? describeChange(enteredNumber) : `Enter a number of at least ${minimum}`}
				</p>
				{errorMessage && <p className='error-text'>{errorMessage}</p>}
				<div className='flex justify-end gap-3'>
					<button type='button' onClick={onClose} className='btn-secondary'>
						Cancel
					</button>
					<button type='submit' disabled={!isValid || isSaving} className='btn-primary'>
						{isSaving ? 'Saving...' : 'Save'}
					</button>
				</div>
			</form>
		</Dialog>
	);
};

export default NumberDialog;
