import { Info } from 'lucide-react';

// The title of an admin page with its buttons on the right, and an optional note about sample data under it
const AdminPageHeader = ({ title, sampleDataNote, children }) => (
	<div className='flex flex-col gap-6'>
		<div className='flex flex-wrap items-end justify-between gap-4'>
			<h1 className='title'>{title}</h1>
			{children && <div className='flex flex-wrap gap-3'>{children}</div>}
		</div>
		{sampleDataNote && (
			<p className='flex items-center gap-3 rounded bg-brand-soft p-4 text-[14px] text-brand-dark'>
				<Info size={18} className='shrink-0' />
				{sampleDataNote}
			</p>
		)}
	</div>
);

export default AdminPageHeader;
