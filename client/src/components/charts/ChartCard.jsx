// A chart in a card with its title and a one line summary, and the same numbers as a table for anyone who cannot read the chart
// rows is [{ label, value }] with the value already formatted
const ChartCard = ({ title, summary, rows, height = 'h-64', className = '', children }) => (
	<section className={`card flex flex-col gap-4 p-6 ${className}`}>
		<div className='flex flex-wrap items-baseline justify-between gap-2'>
			<h2 className='text-[18px]'>{title}</h2>
			{summary && <span className='text-[13px] font-bold text-muted'>{summary}</span>}
		</div>
		<div className={`relative ${height}`}>{children}</div>
		<details className='text-[13px]'>
			<summary className='cursor-pointer text-muted hover:text-ink'>Show the numbers</summary>
			<table className='mt-2 w-full tabular-nums'>
				<tbody>
					{rows.map((row) => (
						<tr key={row.label} className='border-t border-panel'>
							<td className='py-1.5'>{row.label}</td>
							<td className='py-1.5 text-right font-bold'>{row.value}</td>
						</tr>
					))}
				</tbody>
			</table>
		</details>
	</section>
);

export default ChartCard;
