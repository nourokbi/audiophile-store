import { Link } from 'react-router-dom';

// The word mark with the orange dot
const Logo = () => (
	<Link to='/' className='group flex items-center gap-1'>
		<span className='text-[18px] font-bold uppercase tracking-wider'>Audiophile</span>
		<span className='h-2 w-2 rounded-full bg-brand transition-transform group-hover:scale-125' />
	</Link>
);

export default Logo;
