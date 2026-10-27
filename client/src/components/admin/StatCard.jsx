// One headline number with its label, an icon and a short note under it, a warning card gets an orange edge
const StatCard = ({ icon: Icon, label, value, note, isWarning = false }) => (
	<div className={`card flex flex-col gap-3 p-5 ${isWarning ? 'border-l-4 border-brand' : ''}`}>
		<div className='flex items-center justify-between'>
			<span className='field-label text-muted'>{label}</span>
			<span className='flex h-9 w-9 items-center justify-center rounded bg-panel text-brand'>
				<Icon size={18} />
			</span>
		</div>
		<span className={`text-[28px] font-bold leading-none ${isWarning ? 'text-brand' : ''}`}>{value}</span>
		<span className='text-[13px] text-muted'>{note}</span>
	</div>
);

export default StatCard;
