import React from 'react';

import Menu from '../components/Menu';
import BestGear from '../components/BestGear';
import { useParams, Link } from 'react-router-dom';

import data from '../data/data.json';

const Product = (props) => {
	const { slug } = useParams();
	const [productData, setProductData] = React.useState(null);

	React.useEffect(() => {
		const matchedProduct = data.find((product) => product.slug === slug);
		setProductData(matchedProduct);
	}, [slug]);

	if (!productData) {
		return <h2>Loading...</h2>;
	}

	const handleAddToCart = () => {
		const { image, name, price } = productData.cart;
		const productToAdd = {
			image,
			name,
			price,
		};
		props.addToCart(productToAdd);
	};

	const {
		includes,
		name,
		image,
		description,
		features,
		gallery,
		price,
		others,
	} = productData;

	const equipmentArr = includes;
	const equipment = equipmentArr.map((eq, index) => {
		return (
			<div key={index} className='flex gap-6'>
				<span className='font-[500] text-cream text-[15px] leading-[25px]'>
					{eq.quantity}x
				</span>
				<p className='font-[500] text-black/50 text-[15px] leading-[25px]'>
					{eq.item}
				</p>
			</div>
		);
	});

	const othersArr = others;
	const alsoLike = othersArr.map((el, index) => {
		return (
			<div key={index} className='flex flex-col items-center'>
				{/* desktop image */}
				<img
					src={el.image.desktop}
					className='xs:hidden lg:block rounded-[8px]'
				/>
				{/* tablet image */}
				<img
					src={el.image.tablet}
					className='xs:hidden md:block lg:hidden rounded-[8px]'
				/>
				{/* mobile image */}
				<img
					src={el.image.mobile}
					className='xs:block md:hidden rounded-[8px]'
				/>
				<h5 className='mt-10'>{el.name}</h5>
				<Link to={`../${el.category}/${el.slug}`}>
					<button className='btn bg-cream hover:bg-cream-light transition-all duration-150 mt-8'>
						See product
					</button>
				</Link>
			</div>
		);
	});

	return (
		<main className='mt-[95px]'>
			<div className='xs:px-[24px] md:px-[40px] lg:px-[165px] xs:pt-4 md:pt-8 lg:pt-[79px]'>
				<Link relative='path' to='..'>
					<button className='p text-black/50 cursor-pointer hover:text-cream transition-all duration-150'>
						Go back
					</button>
				</Link>
			</div>

			<section className='xs:px-[24px] md:px-[40px] lg:px-[165px] flex xs:flex-col md:flex-row xs:items-start md:items-center lg:min-w-[730px] gap-[10%] xs:mt-6 lg:mt-[56px]'>
				<div>
					{/* desktop image */}
					<img
						src={image.desktop}
						className='xs:hidden lg:block min-w-[443px] w-[540px] rounded-[8px]'
					/>
					{/* tablet image */}
					<img
						src={image.tablet}
						className='xs:hidden md:block lg:hidden rounded-[8px]'
					/>
					{/* mobile image */}
					<img
						src={image.mobile}
						className='xs:block md:hidden rounded-[8px]'
					/>
				</div>

				<div className='md:w-[450px]'>
					{productData.new && (
						<p className='text-[14px] text-cream tracking-[10px] font-normal uppercase xs:mt-8 md:mt-0 xs:mb-6 md:mb-4'>
							New Product
						</p>
					)}
					<h2 className='xs:text-[28px] lg:text-[40px] font-bold xs:leading-normal md:leading-[32px] lg:leading-[44px] xs:tracking-[1px] lg:tracking-[1.4px] uppercase'>
						{name}
					</h2>
					<p className='p text-black/50 xs:py-6 md:py-8'>{description}</p>
					<h6 className='xs:mb-[31px] lg:mb-[47px]'>$ {price}</h6>
					<div className='flex gap-4'>
						<div className='w-[120px] flex justify-between bg-gray-light p-[15px] items-center'>
							<div
								onClick={props.decrease}
								className='text-[14px] text-black/30 hover:text-cream font-bold tracking-[1px] cursor-pointer'
							>
								-
							</div>
							<div className='text-[14px] text-black font-bold tracking-[1px]'>
								{props.quantityToAdd}
							</div>
							<div
								onClick={props.increase}
								className='text-[14px] text-black/30 hover:text-cream font-bold tracking-[1px] cursor-pointer'
							>
								+
							</div>
						</div>
						<button
							onClick={handleAddToCart}
							className='btn bg-cream hover:bg-cream-light transition-all duration-150 whitespace-nowrap'
						>
							Add to cart
						</button>
					</div>
				</div>
			</section>

			<section className='flex xs:flex-col lg:flex-row xs:px-[24px] md:px-[40px] lg:px-[165px] gap-[125px] xs:py-[88px] md:py-[120px] lg:py-[160px]'>
				<div className='flex flex-col gap-[32px] lg:max-w-[635px]'>
					<h3 className='xs:text-[24px] md:text-[32px] font-bold leading-[36px] xs:tracking-[0.9px] md:tracking-[1.1px] uppercase'>
						Features
					</h3>
					<div
						className='p text-black/50'
						dangerouslySetInnerHTML={{
							__html: features.replace(/\n/g, '<br />'),
						}}
					></div>
				</div>
				<div className='flex xs:flex-col md:flex-row lg:flex-col xs:gap-6 md:gap-[30%] lg:gap-8 whitespace-nowrap'>
					<h3 className='xs:text-[24px] md:text-[32px] font-bold leading-[36px] xs:tracking-[0.9px] md:tracking-[1.1px] uppercase'>
						In the box
					</h3>
					<div className='flex flex-col gap-2'>{equipment}</div>
				</div>
			</section>

			<section className='xs:px-[24px] md:px-[40px] lg:px-[165px] flex xs:flex-col md:flex-row xs:gap-5 md:gap-[18px] lg:gap-8 items-center'>
				<div className='flex flex-col xs:gap-5 md:gap-[18px] lg:gap-8'>
					<div>
						{/* desktop image */}
						<img
							src={gallery.first.desktop}
							className='xs:hidden lg:block rounded-[8px]'
						/>
						{/* tablet image */}
						<img
							className='xs:hidden md:block lg:hidden rounded-[8px]'
							src={gallery.first.tablet}
						/>
						{/* mobile image */}
						<img
							src={gallery.first.mobile}
							className='xs:block md:hidden rounded-[8px]'
						/>
					</div>
					<div>
						{/* desktop image */}
						<img
							src={gallery.second.desktop}
							className='xs:hidden lg:block rounded-[8px]'
						/>
						{/* tablet image */}
						<img
							className='xs:hidden md:block lg:hidden rounded-[8px]'
							src={gallery.second.tablet}
						/>
						{/* mobile image */}
						<img
							src={gallery.second.mobile}
							className='xs:block md:hidden rounded-[8px]'
						/>
					</div>
				</div>
				<div>
					{/* desktop image */}
					<img
						src={gallery.third.desktop}
						className='xs:hidden lg:block rounded-[8px]'
					/>
					{/* tablet image */}
					<img
						src={gallery.third.tablet}
						className='xs:hidden md:block lg:hidden rounded-[8px]'
					/>
					{/* mobile image */}
					<img
						src={gallery.third.mobile}
						className='xs:block md:hidden rounded-[8px]'
					/>
				</div>
			</section>

			<section className='flex flex-col items-center xs:px-[24px] md:px-[40px] lg:px-[165px] xs:py-[120px] lg:py-[160px]'>
				<p className='xs:text-[24px] md:text-[32px] font-bold leading-[36px] xs:tracking-[0.9px] md:tracking-[1.1px] uppercase'>
					You may also like
				</p>
				<div className='flex xs:flex-col md:flex-row items-center xs:gap-14 md:gap-[11px] lg:gap-[30px] xs:mt-10 md:mt-14 lg:mt-16'>
					{alsoLike}
				</div>
			</section>

			<section className='xs:px-[24px] md:px-[40px] lg:px-[165px]'>
				<Menu />
			</section>

			<BestGear />
		</main>
	);
};

export default Product;
