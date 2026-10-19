/** @type {import('tailwindcss').Config} */
export default {
	content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
	theme: {
		extend: {
			// The light theme from the Stitch project, reduced to the few colors the shop uses
			colors: {
				brand: '#d87d4a',
				'brand-hover': '#c06a39',
				'brand-dark': '#95491a',
				'brand-soft': '#fbeee6',
				ink: '#1b1c1c',
				muted: '#5f5e5e',
				page: '#fbf9f9',
				panel: '#f5f3f3',
				'panel-strong': '#e9e8e7',
				line: '#e4e2e2',
			},
			fontFamily: {
				sans: ['Manrope', 'sans-serif'],
			},
			maxWidth: {
				page: '1110px',
			},
		},
	},
	plugins: [],
};
