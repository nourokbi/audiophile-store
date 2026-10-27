import { Line } from 'react-chartjs-2';

import { useTheme } from '../../context/ThemeContext';
import { baseOptions, chartColors } from '../../lib/charts';

// One series over time as a 2px line with a light wash under it, a dot shows only on the hovered day
const LineChart = ({ labels, values, formatValue, label }) => {
	const colors = chartColors(useTheme().theme);
	const data = {
		labels,
		datasets: [
			{
				data: values,
				borderColor: colors.series,
				backgroundColor: colors.seriesWash,
				borderWidth: 2,
				fill: 'origin',
				tension: 0.25,
				pointRadius: 0,
				pointHoverRadius: 5,
				pointHoverBackgroundColor: colors.series,
				pointHoverBorderColor: '#ffffff',
				pointHoverBorderWidth: 2,
			},
		],
	};

	return <Line data={data} options={baseOptions(colors, { formatValue })} aria-label={label} role='img' />;
};

export default LineChart;
