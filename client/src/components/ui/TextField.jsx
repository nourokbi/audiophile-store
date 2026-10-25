// The label above a field with its error message on the right, shared by TextField and TextArea
export const FieldHeader = ({ name, label, error }) => (
	<div className='flex items-center justify-between'>
		<label htmlFor={name} className={`field-label ${error ? 'text-red-600' : ''}`}>
			{label}
		</label>
		{error && <span className='text-[12px] font-medium text-red-600'>{error}</span>}
	</div>
);

// A labelled input with its error message, used by login, register, checkout and the admin forms
const TextField = ({ label, name, error, icon: Icon, trailing, className = '', ...inputProps }) => (
	<div className={`flex w-full flex-col gap-2 ${className}`}>
		<FieldHeader name={name} label={label} error={error} />
		<div className='relative flex items-center'>
			{Icon && <Icon size={18} className='pointer-events-none absolute left-4 text-muted' />}
			<input
				id={name}
				name={name}
				className={`input ${Icon ? 'pl-11' : ''} ${trailing ? 'pr-11' : ''} ${error ? 'ring-1 ring-red-600' : ''}`}
				{...inputProps}
			/>
			{trailing && <div className='absolute right-3'>{trailing}</div>}
		</div>
	</div>
);

export default TextField;
