import { Navigate, useNavigate, useParams } from 'react-router-dom';

import Hero from '../components/shop/Hero';
import ProductCard from '../components/shop/ProductCard';
import PromiseStrip from '../components/shop/PromiseStrip';
import ShopFilters from '../components/shop/ShopFilters';
import Pagination from '../components/ui/Pagination';
import { useShopFilters } from '../hooks/useShopFilters';
import { findCategory } from '../lib/navigation';

// The storefront, used for the home page, the new collection and each category page
const Home = ({ collection }) => {
	const { category: categoryKey = 'all' } = useParams();
	const navigate = useNavigate();
	const shop = useShopFilters(categoryKey, collection);

	if (categoryKey !== 'all' && !findCategory(categoryKey)) return <Navigate to='/' replace />;

	const isNewCollection = collection === 'new';
	const heading = isNewCollection ? 'New collection' : findCategory(categoryKey)?.label ?? 'Shop all products';

	const openCategory = (clickedKey) => {
		if (clickedKey !== 'all') navigate(`/${clickedKey}`);
		else navigate(isNewCollection ? '/new' : '/');
	};

	const goToPage = (pageNumber) => {
		shop.setCurrentPage(pageNumber);
		document.getElementById('shop')?.scrollIntoView({ behavior: 'smooth' });
	};

	return (
		<>
			{categoryKey === 'all' && !collection && <Hero />}

			<section id='shop' className='page flex scroll-mt-20 flex-col gap-8 pb-24 pt-12'>
				<div>
					<p className='eyebrow'>{isNewCollection ? 'Just arrived' : 'The collection'}</p>
					<h1 className='title mt-2'>{heading}</h1>
				</div>

				<ShopFilters
					categoryKey={categoryKey}
					onCategoryClick={openCategory}
					filters={shop.filters}
					onFilterChange={shop.setFilter}
					hasFilters={shop.hasFilters}
					onClearFilters={shop.clearFilters}
				/>

				<div className='flex items-center justify-between px-1 text-[14px] text-muted'>
					<span>
						Showing{' '}
						<strong className='text-ink'>
							{shop.pageProducts.length === 0 ? 0 : `${shop.firstIndex + 1} to ${shop.firstIndex + shop.pageProducts.length}`}
						</strong>
					</span>
					{shop.error && <span className='text-red-600'>Products could not be loaded ({shop.error})</span>}
				</div>

				{shop.loading ? (
					<p className='card p-10 text-center text-muted'>Loading products...</p>
				) : shop.pageProducts.length === 0 ? (
					<p className='card p-10 text-center text-muted'>No product matches these filters.</p>
				) : (
					<div
						aria-busy={shop.updating}
						className={`grid grid-cols-1 gap-6 transition-opacity md:grid-cols-2 lg:grid-cols-3 ${shop.updating ? 'opacity-60' : ''}`}
					>
						{shop.pageProducts.map((product) => (
							<ProductCard key={product.slug} product={product} />
						))}
					</div>
				)}

				{shop.totalPages > 1 && (
					<Pagination currentPage={shop.currentPage} totalPages={shop.totalPages} onPageChange={goToPage} />
				)}
			</section>

			<PromiseStrip />
		</>
	);
};

export default Home;
