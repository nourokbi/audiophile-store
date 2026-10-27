// A centered card with an icon, an optional title and text, and the buttons that lead somewhere else
const EmptyState = ({ icon: Icon, title, text, children }) => (
	<div className='card flex flex-col items-center gap-4 p-10 text-center'>
		<Icon size={32} className='text-muted' />
		{title && <h1 className='text-[24px]'>{title}</h1>}
		{text && <p className='max-w-md text-muted'>{text}</p>}
		{children && <div className='flex flex-wrap justify-center gap-3'>{children}</div>}
	</div>
);

export default EmptyState;
