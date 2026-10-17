/** @type {import('tailwindcss').Config} */
export default {
	content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
	theme: {
		screens: {
			xs: '360px',
			sm: '480px',
			md: '768px',
			lg: '976px',
			xl: '1160px',
			xxl: '1440px',
		},
		extend: {
			colors: {
				cream: '#D87D4A',
				'cream-light': '#FBAF85',
				stone: '#101010',
				'gray-light': '#F1F1F1',
				'gray-very-light': '#FAFAFA',
			},
			fontFamily: {
				sans: 'Manrope',
			},
			backgroundImage: {
				'lg-hero-pattern': "url('/assets/home/desktop/image-hero.jpg')",
				'md-hero-pattern': "url('/assets/home/tablet/image-header.jpg')",
				'xs-hero-pattern': "url('/assets/home/mobile/image-header.jpg')",
				circles: "url('/assets/home/desktop/pattern-circles.svg')",
				'xs-ZX7': "url('/assets/home/desktop/image-speaker-zx7.jpg')",
				'md-ZX7': "url('/assets/home/tablet/image-speaker-zx7.jpg')",
				'lg-ZX7': "url('/assets/home/mobile/image-speaker-zx7.jpg')",
				'lg-YX1': "url('/assets/home/desktop/image-earphones-yx1.jpg')",
				'md-YX1': "url('/assets/home/tablet/image-earphones-yx1.jpg')",
				'xs-YX1': "url('/assets/home/mobile/image-earphones-yx1.jpg')",
			},
		},
	},
	plugins: [],
};
