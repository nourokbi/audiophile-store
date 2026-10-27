import { Link } from 'react-router-dom';
import { ArrowRight } from 'lucide-react';

// The flagship banner on top of the home page
const Hero = () => (
	<section className='bg-surface shadow-sm'>
		<div className='page grid grid-cols-1 items-center gap-10 py-12 lg:grid-cols-12 lg:py-20'>
			<div className='flex flex-col gap-5 lg:col-span-7'>
				<p className='eyebrow flex items-center gap-2'>
					<span className='h-2 w-2 rounded-full bg-brand' /> Flagship
				</p>
				<h1 className='text-[40px] leading-[44px] tracking-tight md:text-[56px] md:leading-[58px]'>Sound uncompromised.</h1>
				<p className='max-w-xl text-[16px] leading-relaxed text-muted'>
					The XX99 Mark II headphones bring natural, lifelike audio and exceptional build quality for the passionate music
					enthusiast.
				</p>
				<div className='flex flex-wrap gap-3'>
					<Link to='/headphones/xx99-mark-two-headphones' className='btn-primary'>
						See product <ArrowRight size={16} />
					</Link>
					<Link to='/new' className='btn-secondary'>
						New collection
					</Link>
				</div>
			</div>
			<div className='relative lg:col-span-5'>
				<div className='relative aspect-[4/3] overflow-hidden rounded bg-panel shadow-lg'>
					<img
						src='/assets/product-xx99-mark-two-headphones/desktop/image-product.jpg'
						alt='XX99 Mark II headphones'
						className='h-full w-full object-cover'
					/>
					<div className='absolute bottom-4 left-4 rounded bg-ink px-4 py-2 text-page'>
						<p className='text-[11px] uppercase tracking-widest opacity-75'>Headphones</p>
						<p className='text-[18px] font-bold uppercase'>XX99 Mark II</p>
					</div>
				</div>
			</div>
		</div>
	</section>
);

export default Hero;
