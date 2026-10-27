import { BarElement, CategoryScale, Chart, Filler, LinearScale, LineElement, PointElement, Tooltip } from 'chart.js';

// Only the pieces the dashboard draws, so the bundle stays small, and no legend since every chart is one series
Chart.register(BarElement, CategoryScale, Filler, LinearScale, LineElement, PointElement, Tooltip);

// Chart.js paints on a canvas and cannot read Tailwind classes, so these mirror the tokens in index.css
// The series orange is the brand colour, one step deeper on the dark card so it keeps its lightness band
const THEME_COLORS = {
	light: {
		series: '#d87d4a',
		seriesWash: 'rgba(216, 125, 74, 0.1)',
		grid: 'rgb(228 226 226)',
		text: 'rgb(95 94 94)',
		tooltip: 'rgb(27 28 28)',
	},
	dark: {
		series: '#c96f3e',
		seriesWash: 'rgba(201, 111, 62, 0.1)',
		grid: 'rgb(58 60 60)',
		text: 'rgb(168 165 163)',
		tooltip: 'rgb(52 53 53)',
	},
};

export const chartColors = (theme) => THEME_COLORS[theme === 'dark' ? 'dark' : 'light'];

// 1,284 stays as it is, 12,900 becomes 12.9K, for the axis ticks
const compactNumber = new Intl.NumberFormat('en-US', { notation: 'compact', maximumFractionDigits: 1 });

// The shared look: hairline grid on the value axis only, muted ticks, and a tooltip on hover that shows the full value
export function baseOptions(colors, { formatValue, isHorizontal = false }) {
	const valueAxis = {
		beginAtZero: true,
		border: { display: false },
		grid: { color: colors.grid, lineWidth: 1 },
		ticks: { color: colors.text, precision: 0, callback: (value) => compactNumber.format(value) },
	};
	const labelAxis = {
		border: { color: colors.grid },
		grid: { display: false },
		ticks: { color: colors.text, autoSkip: true, maxRotation: 0 },
	};

	return {
		responsive: true,
		maintainAspectRatio: false,
		indexAxis: isHorizontal ? 'y' : 'x',
		interaction: { mode: 'index', intersect: false },
		scales: isHorizontal ? { x: valueAxis, y: labelAxis } : { x: labelAxis, y: valueAxis },
		plugins: {
			tooltip: {
				backgroundColor: colors.tooltip,
				titleColor: '#ffffff',
				bodyColor: '#ffffff',
				displayColors: false,
				padding: 10,
				callbacks: { label: (context) => formatValue(context.parsed[isHorizontal ? 'x' : 'y']) },
			},
		},
	};
}
