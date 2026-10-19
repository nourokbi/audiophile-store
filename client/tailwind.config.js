// Each color reads a CSS variable from index.css, so the dark theme only swaps the variables
const token = (name) => `rgb(var(--${name}) / <alpha-value>)`;

/** @type {import('tailwindcss').Config} */
export default {
	content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
	darkMode: 'class',
	theme: {
		extend: {
			colors: {
				brand: token('brand'),
				'brand-hover': token('brand-hover'),
				'brand-dark': token('brand-dark'),
				'brand-soft': token('brand-soft'),
				ink: token('ink'),
				muted: token('muted'),
				page: token('page'),
				surface: token('surface'),
				panel: token('panel'),
				'panel-strong': token('panel-strong'),
				line: token('line'),
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
