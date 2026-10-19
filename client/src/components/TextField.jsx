// A labelled input with its error message, used by login, register and checkout
const TextField = ({ label, name, error, icon: Icon, trailing, className = '', ...inputProps }) => (
	<div className={`flex w-full flex-col gap-2 ${className}`}>
		<div className='flex items-center justify-between'>
			<label htmlFor={name} className={`text-[12px] font-bold uppercase tracking-wider ${error ? 'text-red-600' : ''}`}>
				{label}
			</label>
			{error && <span className='text-[12px] font-medium text-red-600'>{error}</span>}
		</div>
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
