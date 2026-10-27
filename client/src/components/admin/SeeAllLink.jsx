import { Link } from 'react-router-dom';
import { ArrowRight } from 'lucide-react';

// The small link in the corner of an overview preview to the full admin page
const SeeAllLink = ({ to }) => (
	<Link to={to} className='label flex shrink-0 items-center gap-1 text-[12px] text-brand-dark hover:underline'>
		See all <ArrowRight size={14} />
	</Link>
);

export default SeeAllLink;
