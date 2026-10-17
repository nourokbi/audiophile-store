import React from 'react';

import Menu from '../components/Menu';
import BestGear from '../components/BestGear';
import { useParams, Link } from 'react-router-dom';

const Category = ({ categories }) => {
	const { category } = useParams();
	const { title, products } = categories[category] || {};

	const reversedProducts = [...products].reverse();

	const elements = reversedProducts.map((product, index) => {
		const isEvenIndex = index % 2 === 0;
		return (
			<div
				className={`${
					isEvenIndex ? 'lg:flex-row' : 'lg:flex-row-reverse'
				} flex xs:flex-col items-center xs:gap-8 md:gap-[52px] lg:gap-[125px] lg:min-w-[730px]`}
				key={product.id}
			>
				<div>
					{/* desktop image */}
					<img
						className='xs:hidden lg:block'
						src={product.categoryImage.desktop}
						alt={product.name}
					/>
					{/* tablet image */}
					<img
						className='xs:hidden md:block lg:hidden '
						src={product.categoryImage.tablet}
						alt={product.name}
					/>
					{/* mobile image */}
					<img
						className='xs:block md:hidden'
						src={product.categoryImage.mobile}
						alt={product.name}
					/>
				</div>
				<div className='xs:text-center lg:text-left'>
					{product.new && (
						<p className='text-[14px] text-cream tracking-[10px] font-normal uppercase mb-4'>
							New Product
						</p>
					)}
					<h2 className='h2'>{product.name}</h2>
					<p className='p text-black/50 xs:mt-6 md:mt-8'>
						{product.description}
					</p>
					<Link to={product.slug}>
						<button className='btn bg-cream hover:bg-cream-light transition-all duration-150 xs:mt-6 lg:mt-10'>
							See Product
						</button>
					</Link>
				</div>
			</div>
		);
	});

	return (
		<main>
			<header className='bg-stone text-white mt-[95px] flex items-center justify-center xs:py-8 md:py-[98px]'>
				<h2 className='xs:text-[24px] md:text-[40px] font-bold xs:leading-normal md:leading-[44px] xs:tracking-[2px] md:tracking-[1.4px] uppercase'>
					{title}
				</h2>
			</header>
			<section className='xs:px-[24px] md:px-[40px] lg:px-[165px] flex flex-col xs:gap-[120px] lg:gap-[160px] xs:pt-16 xs:pb-[120px] md:py-[120px] lg:py-[160px]'>
				{elements}
			</section>
			<section className='xs:px-[24px] md:px-[40px] lg:px-[165px]'>
				<Menu />
			</section>
			<BestGear />
		</main>
	);
};

export default Category;
