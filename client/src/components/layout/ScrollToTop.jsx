import React from 'react';
import { useLocation, useNavigationType } from 'react-router-dom';

// A new page glides up to its top, while Back and Forward leave the browser to put you where you were
// Someone who asked their system for less motion gets the jump instead of the glide
const ScrollToTop = () => {
	const { pathname } = useLocation();
	const navigationType = useNavigationType();

	React.useEffect(() => {
		if (navigationType === 'POP') return;
		const prefersLessMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
		window.scrollTo({ top: 0, behavior: prefersLessMotion ? 'auto' : 'smooth' });
	}, [pathname, navigationType]);

	return null;
};

export default ScrollToTop;
