import React from 'react';

import Dialog from '../Dialog';
import TextField from '../TextField';
import { CATEGORIES } from '../../lib/navigation';

const EMPTY_FORM = { name: '', description: '', features: '', price: '', stock: '0', category: 'headphones', imageUrl: '' };

// The API's category names start with a capital letter, the client keeps them in lower case
const toApiCategory = (categoryKey) => categoryKey[0].toUpperCase() + categoryKey.slice(1);

// The add product form, the checks mirror the API's rules and onSave gets the body the API expects
const ProductDialog = ({ isSaving, errorMessage, onClose, onSave }) => {
	const [form, setForm] = React.useState(EMPTY_FORM);
	const [hasTriedToSave, setHasTriedToSave] = React.useState(false); // errors show after the first save so an empty form is not all red
	const updateField = (fieldName) => (event) =>
		setForm((currentForm) => ({ ...currentForm, [fieldName]: event.target.value }));

	const price = Number(form.price);
	const stock = Number(form.stock);
	const fieldErrors = {
		name: form.name.trim().length < 2 && 'At least 2 characters',
		description: form.description.trim().length < 10 && 'At least 10 characters',
		price: !(price > 0) && 'Above 0',
		stock: !(Number.isInteger(stock) && stock >= 0) && 'A whole number, 0 or more',
	};
	const isFormValid = !Object.values(fieldErrors).some(Boolean);
	const errorToShow = (fieldName) => (hasTriedToSave ? fieldErrors[fieldName] || undefined : undefined);

	const submitForm = (event) => {
		event.preventDefault();
		setHasTriedToSave(true);
		if (!isFormValid) return;
		onSave({
			name: form.name.trim(),
			description: form.description.trim(),
			features: form.features.trim() || null,
			price,
			stock,
			category: toApiCategory(form.category),
			imageUrl: form.imageUrl.trim() || null,
			inTheBox: null,
		});
	};

	return (
		<Dialog title='Add product' onClose={onClose} className='max-w-[560px]'>
			<form onSubmit={submitForm} className='flex flex-col gap-4'>
				<div className='-mx-1 flex max-h-[65vh] flex-col gap-4 overflow-y-auto px-1'>
					<TextField
						label='Name'
						name='name'
						value={form.name}
						onChange={updateField('name')}
						error={errorToShow('name')}
						autoFocus
					/>
					<TextArea
						label='Description'
						name='description'
						value={form.description}
						onChange={updateField('description')}
						error={errorToShow('description')}
					/>
					<TextArea label='Features (optional)' name='features' value={form.features} onChange={updateField('features')} />

					<div className='grid grid-cols-2 gap-4'>
						<TextField
							label='Price (EGP)'
							name='price'
							type='number'
							min='1'
							step='0.01'
							value={form.price}
							onChange={updateField('price')}
							error={errorToShow('price')}
						/>
						<TextField
							label='Stock'
							name='stock'
							type='number'
							min='0'
							step='1'
							value={form.stock}
							onChange={updateField('stock')}
							error={errorToShow('stock')}
						/>
					</div>

					<label className='flex flex-col gap-2'>
						<span className='text-[12px] font-bold uppercase tracking-wider'>Category</span>
						<select value={form.category} onChange={updateField('category')} className='input cursor-pointer'>
							{CATEGORIES.map((category) => (
								<option key={category.key} value={category.key}>
									{category.label}
								</option>
							))}
						</select>
					</label>

					<TextField
						label='Image URL (optional)'
						name='imageUrl'
						type='url'
						placeholder='https://...'
						value={form.imageUrl}
						onChange={updateField('imageUrl')}
					/>
				</div>

				{errorMessage && <p className='text-[14px] font-bold text-red-600'>{errorMessage}</p>}

				<div className='flex justify-end gap-3'>
					<button type='button' onClick={onClose} className='btn-secondary'>
						Cancel
					</button>
					<button type='submit' disabled={isSaving} className='btn-primary'>
						{isSaving ? 'Saving...' : 'Add product'}
					</button>
				</div>
			</form>
		</Dialog>
	);
};

// A labelled multi line input that looks like TextField
const TextArea = ({ label, name, error, ...textareaProps }) => (
	<div className='flex flex-col gap-2'>
		<div className='flex items-center justify-between'>
			<label htmlFor={name} className={`text-[12px] font-bold uppercase tracking-wider ${error ? 'text-red-600' : ''}`}>
				{label}
			</label>
			{error && <span className='text-[12px] font-medium text-red-600'>{error}</span>}
		</div>
		<textarea
			id={name}
			name={name}
			rows={3}
			className={`input h-auto py-3 ${error ? 'ring-1 ring-red-600' : ''}`}
			{...textareaProps}
		/>
	</div>
);

export default ProductDialog;
