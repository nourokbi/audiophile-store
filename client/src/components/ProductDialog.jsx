import React from 'react';

import Dialog from './Dialog';
import TextField from './TextField';
import { adminApi } from '../lib/adminApi';
import { CATEGORIES } from '../lib/navigation';

const EMPTY = { name: '', description: '', features: '', price: '', stock: '0', category: 'headphones', imageUrl: '' };

// The API's category names start with a capital letter, the client keeps them in lower case
const toApiCategory = (key) => key[0].toUpperCase() + key.slice(1);

// The add product form, the checks mirror the API's rules and the API's own message is shown when it still says no
const ProductDialog = ({ onClose, onCreated }) => {
	const [form, setForm] = React.useState(EMPTY);
	const [busy, setBusy] = React.useState(false);
	const [error, setError] = React.useState('');
	const set = (key) => (e) => setForm((f) => ({ ...f, [key]: e.target.value }));

	const price = Number(form.price);
	const stock = Number(form.stock);
	const problems = {
		name: form.name.trim().length < 2 && 'At least 2 characters',
		description: form.description.trim().length < 10 && 'At least 10 characters',
		price: !(price > 0) && 'Above 0',
		stock: !(Number.isInteger(stock) && stock >= 0) && 'A whole number, 0 or more',
	};
	const valid = !Object.values(problems).some(Boolean);
	const [touched, setTouched] = React.useState(false); // errors show after the first save so an empty form is not all red

	const save = async (e) => {
		e.preventDefault();
		setTouched(true);
		if (!valid) return;
		setBusy(true);
		setError('');
		try {
			const product = await adminApi.createProduct({
				name: form.name.trim(),
				description: form.description.trim(),
				features: form.features.trim() || null,
				price,
				stock,
				category: toApiCategory(form.category),
				imageUrl: form.imageUrl.trim() || null,
				inTheBox: null,
			});
			onCreated(product);
		} catch (err) {
			setError(err.message);
			setBusy(false);
		}
	};

	const show = (key) => (touched ? problems[key] || undefined : undefined);

	return (
		<Dialog title='Add product' onClose={onClose} className='max-w-[560px]'>
			<form onSubmit={save} className='flex flex-col gap-4'>
				<div className='-mx-1 flex max-h-[65vh] flex-col gap-4 overflow-y-auto px-1'>
					<TextField label='Name' name='name' value={form.name} onChange={set('name')} error={show('name')} autoFocus />
					<TextArea label='Description' name='description' value={form.description} onChange={set('description')} error={show('description')} />
					<TextArea label='Features (optional)' name='features' value={form.features} onChange={set('features')} />

					<div className='grid grid-cols-2 gap-4'>
						<TextField label='Price (EGP)' name='price' type='number' min='1' step='0.01' value={form.price} onChange={set('price')} error={show('price')} />
						<TextField label='Stock' name='stock' type='number' min='0' step='1' value={form.stock} onChange={set('stock')} error={show('stock')} />
					</div>

					<label className='flex flex-col gap-2'>
						<span className='text-[12px] font-bold uppercase tracking-wider'>Category</span>
						<select value={form.category} onChange={set('category')} className='input cursor-pointer'>
							{CATEGORIES.map((c) => (
								<option key={c.key} value={c.key}>
									{c.label}
								</option>
							))}
						</select>
					</label>

					<TextField label='Image URL (optional)' name='imageUrl' type='url' placeholder='https://...' value={form.imageUrl} onChange={set('imageUrl')} />
				</div>

				{error && <p className='text-[14px] font-bold text-red-600'>{error}</p>}

				<div className='flex justify-end gap-3'>
					<button type='button' onClick={onClose} className='btn-secondary'>
						Cancel
					</button>
					<button type='submit' disabled={busy} className='btn-primary'>
						{busy ? 'Saving...' : 'Add product'}
					</button>
				</div>
			</form>
		</Dialog>
	);
};

// A labelled multi line input that looks like TextField
const TextArea = ({ label, name, error, ...props }) => (
	<div className='flex flex-col gap-2'>
		<div className='flex items-center justify-between'>
			<label htmlFor={name} className={`text-[12px] font-bold uppercase tracking-wider ${error ? 'text-red-600' : ''}`}>
				{label}
			</label>
			{error && <span className='text-[12px] font-medium text-red-600'>{error}</span>}
		</div>
		<textarea id={name} name={name} rows={3} className={`input h-auto py-3 ${error ? 'ring-1 ring-red-600' : ''}`} {...props} />
	</div>
);

export default ProductDialog;
