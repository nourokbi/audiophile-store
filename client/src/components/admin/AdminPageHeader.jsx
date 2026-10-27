// The title of an admin page with its buttons on the right
const AdminPageHeader = ({ title, children }) => (
	<div className='flex flex-wrap items-end justify-between gap-4'>
		<h1 className='title'>{title}</h1>
		{children && <div className='flex flex-wrap gap-3'>{children}</div>}
	</div>
);

export default AdminPageHeader;
