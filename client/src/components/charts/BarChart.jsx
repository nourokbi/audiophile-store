import { Bar } from 'react-chartjs-2';

import { useTheme } from '../../context/ThemeContext';
import { baseOptions, chartColors } from '../../lib/charts';

// One series of bars, columns by default or sideways for long names, thin with a rounded end and a square base
const BarChart = ({ labels, values, formatValue, isHorizontal = false, label }) => {
	const colors = chartColors(useTheme().theme);
	const data = {
		labels,
		datasets: [
			{
				data: values,
				backgroundColor: colors.series,
				maxBarThickness: 24,
				borderRadius: 4,
				borderSkipped: 'start',
			},
		],
	};

	return <Bar data={data} options={baseOptions(colors, { formatValue, isHorizontal })} aria-label={label} role='img' />;
};

export default BarChart;
