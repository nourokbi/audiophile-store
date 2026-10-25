import { FieldHeader } from './TextField';

// A labelled multi line input that looks like TextField
const TextArea = ({ label, name, error, rows = 3, ...textareaProps }) => (
	<div className='flex flex-col gap-2'>
		<FieldHeader name={name} label={label} error={error} />
		<textarea
			id={name}
			name={name}
			rows={rows}
			className={`input h-auto py-3 ${error ? 'ring-1 ring-red-600' : ''}`}
			{...textareaProps}
		/>
	</div>
);

export default TextArea;
