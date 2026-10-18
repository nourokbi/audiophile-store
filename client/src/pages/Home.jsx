import { Link, useSearchParams } from 'react-router-dom';

import BestGear from '../components/BestGear';
import ProductCard from '../components/ProductCard';
import { useCatalog } from '../context/CatalogContext';

import data from '../data/data.json';

const FILTERS = [
	['all', 'All'],
	['headphones', 'Headphones'],
	['speakers', 'Speakers'],
	['earphones', 'Earphones'],
];

// New products first, then the dearest first, so the flagship models lead the grid.
const PRODUCTS = [...data].sort((a, b) => Number(b.new) - Number(a.new) || b.price - a.price);

// The shop's front page: the hero, then every product as a card you can buy from directly.
// The filter is kept in the URL (?category=speakers), so a filtered view can be shared or reloaded.
const Home = () => {
	const { bySlug, loading, error } = useCatalog();
	const [params, setParams] = useSearchParams();
	const active = FILTERS.some(([key]) => key === params.get('category')) ? params.get('category') : 'all';
	const shown = active === 'all' ? PRODUCTS : PRODUCTS.filter((p) => p.category === active);

	const choose = (key) => setParams(key === 'all' ? {} : { category: key }, { replace: true });

	return (
		<main>
			<header className='xs:bg-xs-hero-pattern md:bg-md-hero-pattern mt-[95px] lg:bg-lg-hero-pattern bg-no-repeat bg-center bg-cover md:px-[40px] lg:px-[165px] w-full'>
				<div className='border-t-[1px] border-white/40 border-solid xs:pt-[96px] lg:pt-[128px] xs:pb-[112px] lg:pb-[158px] flex flex-col justify-center xs:items-center xs:text-center lg:items-start lg:text-left'>
					<div className='max-w-[398px] xs:px-6 md:px-0'>
						<p className='text-white/50 font-normal uppercase tracking-[10px]'>New Product</p>
						<h1 className='xs:text-[36px] xs:leading-[40px] xs:tracking-[1.3px] md:text-[56px] md:leading-[58px] md:tracking-[2px] text-white mt-6'>
							XX99 Mark II Headphones
						</h1>
						<p className='mt-6 text-white/75 lg:w-[360px]'>
							Experience natural, lifelike audio and exceptional build quality made for the passionate music
							enthusiast.
						</p>
						<div className='flex xs:justify-center lg:justify-start gap-4 mt-10'>
							<Link to='headphones/xx99-mark-two-headphones'>
								<button className='btn bg-cream hover:bg-cream-light transition-all duration-150'>See Product</button>
							</Link>
							<a href='#shop'>
								<button className='btn bg-transparent border border-white hover:bg-white hover:text-black transition-all duration-150'>
									Shop all
								</button>
							</a>
						</div>
					</div>
				</div>
			</header>

			<section id='shop' className='bg-gray-light scroll-mt-[95px]'>
				<div className='xs:px-[24px] md:px-[40px] lg:px-[165px] xs:py-[64px] md:py-[96px] lg:py-[120px]'>
					<div className='flex xs:flex-col md:flex-row md:items-end justify-between gap-6'>
						<div>
							<p className='text-cream text-[13px] font-bold uppercase leading-[25px] tracking-wide'>The collection</p>
							<h2 className='xs:text-[28px] md:text-[40px] mt-2'>Shop all products</h2>
						</div>
						<div role='tablist' aria-label='Filter by category' className='flex flex-wrap gap-2'>
							{FILTERS.map(([key, label]) => (
								<button
									key={key}
									role='tab'
									aria-selected={active === key}
									onClick={() => choose(key)}
									className={`text-[13px] uppercase font-bold tracking-[1px] px-4 py-2 rounded-full border transition-all duration-150 ${
										active === key
											? 'bg-stone border-stone text-white'
											: 'bg-white border-stone-300 text-black/60 hover:border-cream hover:text-cream'
									}`}
								>
									{label}
								</button>
							))}
						</div>
					</div>

					{error && (
						<p className='p text-red-600 mt-6'>
							Prices and stock couldn’t be loaded ({error}). Is the API running on port 5040?
						</p>
					)}

					<div className='mt-10 grid xs:grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6'>
						{shown.map((product) => (
							<ProductCard key={product.slug} product={product} live={bySlug[product.slug]} loading={loading} />
						))}
					</div>
				</div>
			</section>

			<BestGear />
		</main>
	);
};

export default Home;
