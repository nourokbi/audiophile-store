import TextField from '../ui/TextField';

// The new address fields, the checkout page holds the values and the errors
const AddressForm = ({ form, fieldErrors, onFieldChange }) => (
	<div className='flex flex-col gap-5'>
		<div className='grid grid-cols-1 gap-5 md:grid-cols-2'>
			<TextField
				label='Full name'
				name='name'
				autoComplete='name'
				value={form.name}
				onChange={onFieldChange}
				error={fieldErrors.name}
			/>
			<TextField
				label='Phone number'
				name='phone'
				type='tel'
				autoComplete='tel'
				placeholder='+20 100 000 0000'
				value={form.phone}
				onChange={onFieldChange}
				error={fieldErrors.phone}
			/>
		</div>
		<TextField
			label='Street address'
			name='street'
			autoComplete='street-address'
			placeholder='12 Tahrir Street'
			value={form.street}
			onChange={onFieldChange}
			error={fieldErrors.street}
		/>
		<div className='grid grid-cols-1 gap-5 md:grid-cols-3'>
			<TextField
				label='City'
				name='city'
				autoComplete='address-level2'
				placeholder='Cairo'
				value={form.city}
				onChange={onFieldChange}
				error={fieldErrors.city}
			/>
			<TextField
				label='Postal code (optional)'
				name='postalCode'
				autoComplete='postal-code'
				placeholder='11511'
				maxLength={20}
				value={form.postalCode}
				onChange={onFieldChange}
			/>
			<TextField
				label='Country'
				name='country'
				autoComplete='country-name'
				value={form.country}
				onChange={onFieldChange}
				error={fieldErrors.country}
			/>
		</div>
		<p className='text-[13px] text-muted'>This address is saved to your account for next time.</p>
	</div>
);

export default AddressForm;
