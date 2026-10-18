// The labelled input from the checkout design: label on the left, error on the right,
// red border when invalid. Used by login, register and checkout.
const TextField = ({ label, name, error, className = '', ...inputProps }) => {
	return (
		<div className={`flex flex-col w-full ${className}`}>
			<div className='flex items-center justify-between'>
				<label htmlFor={name} className={`text-xs font-bold ${error ? 'text-red-600' : 'text-black'}`}>
					{label}
				</label>
				<p className='text-red-600 text-xs font-medium'>{error}</p>
			</div>
			<input
				id={name}
				name={name}
				className={`placeholder:text-[14px] placeholder:text-black/40 placeholder:font-bold text-black font-bold py-[18px] px-[24px] bg-white rounded-lg border border-stone-300 focus:outline-cream caret-cream mt-[9px] disabled:bg-gray-very-light disabled:text-black/50 ${
					error ? 'border-red-600' : ''
				}`}
				{...inputProps}
			/>
		</div>
	);
};

export default TextField;
