const BADGE_COLORS = {
	Pending: 'bg-panel text-muted',
	Confirmed: 'bg-brand-soft text-brand-dark',
	Processing: 'bg-brand-soft text-brand-dark',
	Shipped: 'bg-ink text-page',
	Delivered: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300',
	Cancelled: 'bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-300',
};

// The order status as a small coloured pill
const StatusBadge = ({ status }) => (
	<span className={`rounded-full px-3 py-1 text-[11px] font-bold uppercase tracking-widest ${BADGE_COLORS[status] ?? ''}`}>
		{status}
	</span>
);

export default StatusBadge;
