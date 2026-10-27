import React from 'react';

import BoxContentsEditor from './BoxContentsEditor';
import Dialog from '../ui/Dialog';
import Select from '../ui/Select';
import TextArea from '../ui/TextArea';
import TextField from '../ui/TextField';
import { CATEGORIES } from '../../lib/navigation';

const EMPTY_FORM = { name: '', description: '', features: '', price: '', stock: '0', category: 'headphones', imageUrl: '' };

const MAX_ITEM_LENGTH = 100; // the API's limit for an item name

// A picture from the shop's own files like /assets/products/name.jpg, or a full link to another site
const isImageAddress = (text) => text === '' || text.startsWith('/') || /^https?:\/\/\S+$/i.test(text);

// One editable row of the box, rowId keeps React's key steady while the name is typed
const newBoxRow = (item = '', quantity = '1') => ({ rowId: crypto.randomUUID(), item, quantity });

// The rows that hold something, a row left fully empty is ignored instead of being an error
const filledRows = (rows) => rows.filter((row) => row.item.trim() !== '' || row.quantity.trim() !== '');

function findBoxError(rows) {
	const rowsToSave = filledRows(rows);
	if (rowsToSave.some((row) => row.item.trim() === '')) return 'Every item needs a name';
	if (rowsToSave.some((row) => !(Number.isInteger(Number(row.quantity)) && Number(row.quantity) >= 1)))
		return 'Quantities are whole numbers, 1 or more';
	if (rowsToSave.some((row) => row.item.trim().length > MAX_ITEM_LENGTH)) return `Names up to ${MAX_ITEM_LENGTH} characters`;
	const names = rowsToSave.map((row) => row.item.trim().toLowerCase());
	if (new Set(names).size !== names.length) return 'Each item once, add up the quantity instead';
	return false;
}

// The API keeps the box as { "Headphone unit": 1, "User manual": 1 }
const toInTheBox = (rows) => Object.fromEntries(filledRows(rows).map((row) => [row.item.trim(), Number(row.quantity)]));

// The API's category names start with a capital letter, the client keeps them in lower case
const toApiCategory = (categoryKey) => categoryKey[0].toUpperCase() + categoryKey.slice(1);

// The form a product starts from when it is edited, numbers become text like the inputs hold them
const toForm = (product) => ({
	name: product.name,
	description: product.description,
	features: product.features ?? '',
	price: String(product.price),
	stock: String(product.stock),
	category: product.category,
	imageUrl: product.imageUrl ?? '',
});

// The add and edit product form, the checks mirror the API's rules and onSave gets the body the API expects
// An edit leaves the stock out, it only changes through Restock so an old number here cannot undo a checkout
const ProductDialog = ({ product = null, isSaving, errorMessage, onClose, onSave }) => {
	const isEditing = product !== null;
	const [form, setForm] = React.useState(() => (isEditing ? toForm(product) : EMPTY_FORM));
	const [boxRows, setBoxRows] = React.useState(() =>
		isEditing ? product.includes.map((entry) => newBoxRow(entry.item, String(entry.quantity))) : [],
	);
	const [hasTriedToSave, setHasTriedToSave] = React.useState(false); // errors show after the first save so an empty form is not all red
	const updateField = (fieldName) => (event) => setForm((currentForm) => ({ ...currentForm, [fieldName]: event.target.value }));

	const updateBoxRow = (rowId, fieldName, value) =>
		setBoxRows((currentRows) => currentRows.map((row) => (row.rowId === rowId ? { ...row, [fieldName]: value } : row)));
	const addBoxRow = () => setBoxRows((currentRows) => [...currentRows, newBoxRow()]);
	const removeBoxRow = (rowId) => setBoxRows((currentRows) => currentRows.filter((row) => row.rowId !== rowId));

	const price = Number(form.price);
	const stock = Number(form.stock);
	const fieldErrors = {
		name: form.name.trim().length < 2 && 'At least 2 characters',
		description: form.description.trim().length < 10 && 'At least 10 characters',
		price: !(price > 0) && 'Above 0',
		stock: !(Number.isInteger(stock) && stock >= 0) && 'A whole number, 0 or more',
		box: findBoxError(boxRows),
		imageUrl: !isImageAddress(form.imageUrl.trim()) && 'A path like /assets/... or a link with https://',
	};
	const isFormValid = !Object.values(fieldErrors).some(Boolean);
	const errorToShow = (fieldName) => (hasTriedToSave ? fieldErrors[fieldName] || undefined : undefined);

	const submitForm = (event) => {
		event.preventDefault();
		setHasTriedToSave(true);
		if (!isFormValid) return;
		const details = {
			name: form.name.trim(),
			description: form.description.trim(),
			features: form.features.trim() || null,
			price,
			category: toApiCategory(form.category),
			imageUrl: form.imageUrl.trim() || null,
			inTheBox: toInTheBox(boxRows), // always sent, so removing every row on an edit empties the box
		};
		onSave(isEditing ? details : { ...details, stock });
	};

	return (
		<Dialog title={isEditing ? `Edit ${product.name}` : 'Add product'} onClose={onClose} className='max-w-[560px]'>
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
						{isEditing ? (
							<div className='flex flex-col gap-2'>
								<span className='field-label'>Stock</span>
								<p className='flex h-12 items-center text-[14px] text-muted'>{product.stock} left, change it with Restock</p>
							</div>
						) : (
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
						)}
					</div>

					<Select label='Category' value={form.category} onChange={updateField('category')}>
						{CATEGORIES.map((category) => (
							<option key={category.key} value={category.key}>
								{category.label}
							</option>
						))}
					</Select>

					<BoxContentsEditor
						rows={boxRows}
						error={errorToShow('box')}
						onRowChange={updateBoxRow}
						onAddRow={addBoxRow}
						onRemoveRow={removeBoxRow}
					/>

					<TextField
						label='Image (optional)'
						name='imageUrl'
						inputMode='url'
						placeholder='/assets/products/name.jpg or https://...'
						value={form.imageUrl}
						onChange={updateField('imageUrl')}
						error={errorToShow('imageUrl')}
					/>
					{form.imageUrl.trim() !== '' && isImageAddress(form.imageUrl.trim()) && (
						<img
							src={form.imageUrl.trim()}
							alt='Preview of the product picture'
							className='h-24 w-24 rounded bg-panel object-cover'
							onError={(event) => (event.currentTarget.style.visibility = 'hidden')}
							onLoad={(event) => (event.currentTarget.style.visibility = 'visible')}
						/>
					)}
				</div>

				{errorMessage && <p className='error-text'>{errorMessage}</p>}

				<div className='flex justify-end gap-3'>
					<button type='button' onClick={onClose} className='btn-secondary'>
						Cancel
					</button>
					<button type='submit' disabled={isSaving} className='btn-primary'>
						{isSaving ? 'Saving...' : isEditing ? 'Save changes' : 'Add product'}
					</button>
				</div>
			</form>
		</Dialog>
	);
};

export default ProductDialog;
