import { AudioLines, Cable, Ear, Headphones, Speaker } from 'lucide-react';

// The product categories in menu order, the key matches the API's category in lower case
export const CATEGORIES = [
	{ key: 'headphones', label: 'Headphones', icon: Headphones },
	{ key: 'speakers', label: 'Speakers', icon: Speaker },
	{ key: 'earphones', label: 'Earphones', icon: Ear },
	{ key: 'amplifiers', label: 'Amps and DACs', icon: AudioLines },
	{ key: 'accessories', label: 'Accessories', icon: Cable },
];

export const findCategory = (key) => CATEGORIES.find((c) => c.key === key);

export const navLinkClass = ({ isActive }) =>
	`label transition-colors ${isActive ? 'text-brand-dark' : 'text-muted hover:text-ink'}`;
