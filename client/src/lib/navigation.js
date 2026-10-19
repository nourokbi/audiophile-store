// The shop's main links, shared by the header and the footer
export const NAV_LINKS = [
	{ to: '/', label: 'Home' },
	{ to: '/headphones', label: 'Headphones' },
	{ to: '/speakers', label: 'Speakers' },
	{ to: '/earphones', label: 'Earphones' },
	{ to: '/orders', label: 'My orders' },
];

export const CATEGORIES = ['headphones', 'speakers', 'earphones'];

export const navLinkClass = ({ isActive }) =>
	`label transition-colors ${isActive ? 'text-brand-dark' : 'text-muted hover:text-ink'}`;
