import { NavLink } from 'react-router-dom';
import { Headphones, Speaker, AudioLines } from 'lucide-react';

import Logo from './Logo';
import { NAV_LINKS, navLinkClass } from '../lib/navigation';

const Footer = () => (
	<footer className='bg-panel pb-12 pt-16'>
		<div className='page flex flex-col gap-12'>
			<div className='flex flex-col justify-between gap-8 lg:flex-row lg:items-center'>
				<Logo />
				<nav className='flex flex-wrap items-center gap-6'>
					{NAV_LINKS.map((link) => (
						<NavLink key={link.to} to={link.to} end className={navLinkClass}>
							{link.label}
						</NavLink>
					))}
				</nav>
			</div>
			<p className='max-w-xl text-[15px] leading-relaxed text-muted'>
				Audiophile is an all in one stop to fulfill your audio needs. We are a small team of music lovers and sound
				specialists devoted to helping you get the most out of personal audio.
			</p>
			<div className='flex flex-col items-center justify-between gap-4 sm:flex-row'>
				<p className='text-[13px] text-muted'>Copyright {new Date().getFullYear()}. All rights reserved.</p>
				<div className='flex items-center gap-5 text-muted'>
					<AudioLines size={20} />
					<Headphones size={20} />
					<Speaker size={20} />
				</div>
			</div>
		</div>
	</footer>
);

export default Footer;
