// A Cairo day from the stats API like 2026-10-25 as Oct 25, read as a plain date so the browser's zone cannot shift it
export const dayLabel = (day) =>
	new Date(`${day}T00:00:00Z`).toLocaleDateString('en-US', { month: 'short', day: 'numeric', timeZone: 'UTC' });

// The revenue against the same number of days before, a percent needs something to compare with
export function revenueChangeNote({ revenue, previousRevenue, days }) {
	if (previousRevenue === 0) return revenue > 0 ? `No sales in the ${days} days before` : 'No sales yet';
	const percent = Math.round(((revenue - previousRevenue) / previousRevenue) * 1000) / 10;
	return `${percent >= 0 ? '+' : ''}${percent}% on the ${days} days before`;
}

export const sumOf = (entries, field) => entries.reduce((sum, entry) => sum + entry[field], 0);
