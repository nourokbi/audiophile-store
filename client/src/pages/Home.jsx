import React from 'react';
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom';
import { ArrowRight, BadgeCheck, ChevronDown, ChevronLeft, ChevronRight, CreditCard, PackageX, RotateCcw, Search, Truck } from 'lucide-react';

import ProductCard from '../components/ProductCard';
import { useCatalog } from '../context/CatalogContext';
import { CATEGORIES, findCategory } from '../lib/navigation';

const PRICES = [
	{ key: 'all', label: 'Any price', min: 0, max: Infinity },
	{ key: 'under-1000', label: 'Under EGP 1,000', min: 0, max: 999 },
	{ key: '1000-3000', label: 'EGP 1,000 to 3,000', min: 1000, max: 3000 },
	{ key: '3000-5000', label: 'EGP 3,000 to 5,000', min: 3000, max: 5000 },
	{ key: 'over-5000', label: 'Over EGP 5,000', min: 5000, max: Infinity },
];

const SORTS = {
	featured: { label: 'Featured', compare: (a, b) => Number(b.isNew) - Number(a.isNew) || b.price - a.price },
	'price-asc': { label: 'Price, low to high', compare: (a, b) => a.price - b.price },
	'price-desc': { label: 'Price, high to low', compare: (a, b) => b.price - a.price },
	name: { label: 'Name, A to Z', compare: (a, b) => a.name.localeCompare(b.name) },
};

const PROMISES = [
	{ icon: Truck, title: 'Free shipping', text: 'On every order of EGP 5,000 or more' },
	{ icon: CreditCard, title: 'Secure payment', text: 'Card details stay on Fawaterak, never on our servers' },
	{ icon: PackageX, title: 'Easy cancel', text: 'Cancel any order before it ships' },
	{ icon: BadgeCheck, title: 'Real stock', text: 'What you see is what we have, reserved when you order' },
];

const EMPTY_FILTERS = { search: '', price: 'all', sort: 'featured', inStockOnly: false };
const PAGE_SIZE = 9;

// The storefront, used for the home page, the new collection and each category page
const Home = ({ collection }) => {
	const { category = 'all' } = useParams();
	const navigate = useNavigate();
	const { products, loading, error } = useCatalog();
	const [filters, setFilters] = React.useState(EMPTY_FILTERS);
	const [page, setPage] = React.useState(1);

	// A new category or collection starts again on the first page
	React.useEffect(() => setPage(1), [category, collection]);

	if (category !== 'all' && !findCategory(category)) return <Navigate to='/' replace />;

	const set = (key) => (e) => {
		setFilters({ ...filters, [key]: e.target.type === 'checkbox' ? e.target.checked : e.target.value });
		setPage(1);
	};
	const price = PRICES.find((p) => p.key === filters.price);
	const term = filters.search.trim().toLowerCase();

	const shown = products
		.filter(
			(p) =>
				(collection !== 'new' || p.isNew) &&
				(category === 'all' || p.category === category) &&
				(!filters.inStockOnly || p.inStock) &&
				p.price >= price.min &&
				p.price <= price.max &&
				(!term || p.name.toLowerCase().includes(term) || p.description.toLowerCase().includes(term)),
		)
		.sort(SORTS[filters.sort].compare);

	const pages = Math.max(1, Math.ceil(shown.length / PAGE_SIZE));
	const current = Math.min(page, pages);
	const first = (current - 1) * PAGE_SIZE;
	const onPage = shown.slice(first, first + PAGE_SIZE);

	const goTo = (next) => {
		setPage(next);
		document.getElementById('shop')?.scrollIntoView({ behavior: 'smooth' });
	};

	const heading = collection === 'new' ? 'New collection' : findCategory(category)?.label ?? 'Shop all products';
	const filtered = JSON.stringify(filters) !== JSON.stringify(EMPTY_FILTERS);

	return (
		<>
			{category === 'all' && !collection && <Hero />}

			<section id='shop' className='page flex scroll-mt-20 flex-col gap-8 pb-24 pt-12'>
				<div>
					<p className='eyebrow'>{collection === 'new' ? 'Just arrived' : 'The collection'}</p>
					<h1 className='title mt-2'>{heading}</h1>
				</div>

				<div className='card flex flex-col gap-5 p-6'>
					<div className='flex flex-wrap gap-2'>
						{[{ key: 'all', label: 'All items' }, ...CATEGORIES].map(({ key, label }) => (
							<button
								key={key}
								onClick={() => navigate(key === 'all' ? (collection === 'new' ? '/new' : '/') : `/${key}`)}
								className={`chip ${category === key ? 'bg-brand text-white shadow-sm' : 'bg-panel hover:bg-panel-strong'}`}
							>
								{label}
							</button>
						))}
					</div>

					<div className='grid grid-cols-1 gap-3 md:grid-cols-12'>
						<div className='relative flex items-center md:col-span-6'>
							<Search size={18} className='pointer-events-none absolute left-4 text-muted' />
							<input
								type='search'
								value={filters.search}
								onChange={set('search')}
								placeholder='Search products'
								className='input pl-11'
							/>
						</div>
						<Select value={filters.price} onChange={set('price')} className='md:col-span-3' label='Price'>
							{PRICES.map((p) => (
								<option key={p.key} value={p.key}>
									{p.label}
								</option>
							))}
						</Select>
						<Select value={filters.sort} onChange={set('sort')} className='md:col-span-3' label='Sort'>
							{Object.entries(SORTS).map(([key, s]) => (
								<option key={key} value={key}>
									{s.label}
								</option>
							))}
						</Select>
					</div>

					<div className='flex flex-wrap items-center justify-between gap-4'>
						<label className='flex cursor-pointer select-none items-center gap-3'>
							<input type='checkbox' className='peer sr-only' checked={filters.inStockOnly} onChange={set('inStockOnly')} />
							<span className="relative h-6 w-11 rounded-full bg-panel-strong transition-colors after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-transform after:content-[''] peer-checked:bg-brand peer-checked:after:translate-x-5" />
							<span className='label'>In stock only</span>
						</label>
						{filtered && (
							<button
								onClick={() => {
									setFilters(EMPTY_FILTERS);
									setPage(1);
								}}
								className='label flex items-center gap-2 text-muted hover:text-brand-dark'
							>
								<RotateCcw size={14} /> Clear filters
							</button>
						)}
					</div>
				</div>

				<div className='flex items-center justify-between px-1 text-[14px] text-muted'>
					<span>
						Showing{' '}
						<strong className='text-ink'>
							{onPage.length === 0 ? 0 : `${first + 1} to ${first + onPage.length}`}
						</strong>{' '}
						of {shown.length} products
					</span>
					{error && <span className='text-red-600'>Products could not be loaded ({error})</span>}
				</div>

				{loading ? (
					<p className='card p-10 text-center text-muted'>Loading products...</p>
				) : shown.length === 0 ? (
					<p className='card p-10 text-center text-muted'>No product matches these filters.</p>
				) : (
					<div className='grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3'>
						{onPage.map((product) => (
							<ProductCard key={product.slug} product={product} />
						))}
					</div>
				)}

				{pages > 1 && <Pagination current={current} pages={pages} onChange={goTo} />}
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

// Previous, every page number and next, the current page is orange
const Pagination = ({ current, pages, onChange }) => {
	const square = 'flex h-11 w-11 items-center justify-center rounded shadow-sm transition-colors disabled:opacity-40';
	return (
		<nav aria-label='Pages' className='flex items-center justify-center gap-2 pt-4'>
			<button aria-label='Previous page' disabled={current === 1} onClick={() => onChange(current - 1)} className={`${square} bg-surface hover:bg-panel`}>
				<ChevronLeft size={18} />
			</button>
			{Array.from({ length: pages }, (_, i) => i + 1).map((n) => (
				<button
					key={n}
					aria-current={n === current ? 'page' : undefined}
					onClick={() => onChange(n)}
					className={`${square} label ${n === current ? 'bg-brand text-white' : 'bg-surface hover:bg-panel'}`}
				>
					{n}
				</button>
			))}
			<button aria-label='Next page' disabled={current === pages} onClick={() => onChange(current + 1)} className={`${square} bg-surface hover:bg-panel`}>
				<ChevronRight size={18} />
			</button>
		</nav>
	);
};

const Select = ({ label, className, children, ...props }) => (
	<div className={`relative flex items-center ${className}`}>
		<select aria-label={label} className='input cursor-pointer appearance-none pr-10' {...props}>
			{children}
		</select>
		<ChevronDown size={18} className='pointer-events-none absolute right-4 text-muted' />
	</div>
);

const Hero = () => (
	<section className='bg-surface shadow-sm'>
		<div className='page grid grid-cols-1 items-center gap-10 py-12 lg:grid-cols-12 lg:py-20'>
			<div className='flex flex-col gap-5 lg:col-span-7'>
				<p className='eyebrow flex items-center gap-2'>
					<span className='h-2 w-2 rounded-full bg-brand' /> Flagship
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

export default Home;
