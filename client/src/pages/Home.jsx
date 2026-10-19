import React from 'react';
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom';
import { ArrowRight, BadgeCheck, CreditCard, PackageX, Search, Truck } from 'lucide-react';

import ProductCard from '../components/ProductCard';
import { useCatalog } from '../context/CatalogContext';
import { CATEGORIES } from '../lib/navigation';

const PROMISES = [
	{ icon: Truck, title: 'Free shipping', text: 'On every order of EGP 5,000 or more' },
	{ icon: CreditCard, title: 'Secure payment', text: 'Card details stay on Fawaterak, never on our servers' },
	{ icon: PackageX, title: 'Easy cancel', text: 'Cancel any order before it ships' },
	{ icon: BadgeCheck, title: 'Real stock', text: 'What you see is what we have, reserved when you order' },
];

// The storefront, used for the home page and for each category page
const Home = () => {
	const { category = 'all' } = useParams();
	const navigate = useNavigate();
	const { products, loading, error } = useCatalog();
	const [search, setSearch] = React.useState('');
	const [inStockOnly, setInStockOnly] = React.useState(false);

	if (category !== 'all' && !CATEGORIES.includes(category)) return <Navigate to='/' replace />;

	const term = search.trim().toLowerCase();
	const shown = products.filter(
		(p) =>
			(category === 'all' || p.category === category) &&
			(!inStockOnly || p.inStock) &&
			(!term || p.name.toLowerCase().includes(term) || p.description.toLowerCase().includes(term)),
	);

	return (
		<>
			{category === 'all' && <Hero />}

			<section className='page flex flex-col gap-8 pb-24 pt-12'>
				<div className='card flex flex-col gap-6 p-6'>
					<div className='flex flex-wrap items-center justify-between gap-4'>
						<div className='flex flex-wrap gap-2'>
							{['all', ...CATEGORIES].map((key) => (
								<button
									key={key}
									onClick={() => navigate(key === 'all' ? '/' : `/${key}`)}
									className={`chip ${category === key ? 'bg-brand text-white shadow-sm' : 'bg-panel hover:bg-panel-strong'}`}
								>
									{key === 'all' ? 'All items' : key}
								</button>
							))}
						</div>
						<label className='flex cursor-pointer select-none items-center gap-3'>
							<input
								type='checkbox'
								className='peer sr-only'
								checked={inStockOnly}
								onChange={(e) => setInStockOnly(e.target.checked)}
							/>
							<span className="relative h-6 w-11 rounded-full bg-panel-strong transition-colors after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-transform after:content-[''] peer-checked:bg-brand peer-checked:after:translate-x-5" />
							<span className='label'>In stock only</span>
						</label>
					</div>
					<div className='relative flex items-center'>
						<Search size={18} className='pointer-events-none absolute left-4 text-muted' />
						<input
							type='search'
							value={search}
							onChange={(e) => setSearch(e.target.value)}
							placeholder='Search headphones, speakers and earphones'
							className='input pl-11'
						/>
					</div>
				</div>

				<div className='flex items-center justify-between px-1 text-[14px] text-muted'>
					<span>
						Showing <strong className='text-ink'>{shown.length}</strong> of {products.length} products
					</span>
					{error && <span className='text-red-600'>Prices and stock could not be loaded ({error})</span>}
				</div>

				{shown.length === 0 ? (
					<p className='card p-10 text-center text-muted'>No product matches these filters.</p>
				) : (
					<div className='grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3'>
						{shown.map((product) => (
							<ProductCard key={product.slug} product={product} loading={loading} />
						))}
					</div>
				)}
			</section>

			<section className='bg-panel py-16'>
				<div className='page grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-4'>
					{PROMISES.map(({ icon: Icon, title, text }) => (
						<div key={title} className='card flex flex-col gap-3 p-6'>
							<span className='flex h-12 w-12 items-center justify-center rounded bg-panel text-brand'>
								<Icon size={24} />
							</span>
							<h3 className='text-[18px] tracking-wider'>{title}</h3>
							<p className='text-[14px] text-muted'>{text}</p>
						</div>
					))}
				</div>
			</section>
		</>
	);
};

const Hero = () => (
	<section className='bg-white shadow-sm'>
		<div className='page grid grid-cols-1 items-center gap-10 py-12 lg:grid-cols-12 lg:py-20'>
			<div className='flex flex-col gap-5 lg:col-span-7'>
				<p className='eyebrow flex items-center gap-2'>
					<span className='h-2 w-2 rounded-full bg-brand' /> New product
				</p>
				<h1 className='text-[40px] leading-[44px] tracking-tight md:text-[56px] md:leading-[58px]'>
					Sound uncompromised.
				</h1>
				<p className='max-w-xl text-[16px] leading-relaxed text-muted'>
					The XX99 Mark II headphones bring natural, lifelike audio and exceptional build quality for the passionate
					music enthusiast.
				</p>
				<div className='flex flex-wrap gap-3'>
					<Link to='/headphones/xx99-mark-two-headphones' className='btn-primary'>
						See product <ArrowRight size={16} />
					</Link>
					<a href='#shop' className='btn-secondary'>
						Shop all
					</a>
				</div>
			</div>
			<div className='relative lg:col-span-5'>
				<div className='relative aspect-[4/3] overflow-hidden rounded bg-panel shadow-lg'>
					<img
						src='/assets/product-xx99-mark-two-headphones/desktop/image-product.jpg'
						alt='XX99 Mark II headphones'
						className='h-full w-full object-cover'
					/>
					<span className='label absolute right-4 top-4 flex items-center gap-2 rounded bg-white/90 px-3 py-1 text-[11px] shadow-sm'>
						<span className='h-2 w-2 animate-pulse rounded-full bg-brand' /> Flagship
					</span>
					<div className='absolute bottom-4 left-4 rounded bg-ink px-4 py-2 text-white'>
						<p className='text-[11px] uppercase tracking-widest opacity-75'>Headphones</p>
						<p className='text-[18px] font-bold uppercase'>XX99 Mark II</p>
					</div>
				</div>
			</div>
		</div>
		<span id='shop' className='block scroll-mt-20' />
	</section>
);

export default Home;
