import React from 'react';
import { Link, Navigate, useParams } from 'react-router-dom';
import { ArrowLeft, ShoppingBag } from 'lucide-react';

import ProductCard from '../components/shop/ProductCard';
import ProductGallery from '../components/shop/ProductGallery';
import QuantityStepper from '../components/ui/QuantityStepper';
import { useCatalogProduct, useRelatedProducts } from '../hooks/useCatalog';
import { useAddToCart } from '../hooks/useAddToCart';
import { formatPrice } from '../lib/format';
import { findCategory } from '../lib/navigation';
import { isFewLeft, stockLabel } from '../lib/productHelpers';

const Product = () => {
	const { slug } = useParams();
	const { data: product, isPending, error } = useCatalogProduct(slug);
	const relatedProducts = useRelatedProducts(product?.id);
	const { addToCart, busy, notice } = useAddToCart();
	const [quantity, setQuantity] = React.useState(1);

	// A new product page starts again at one
	React.useEffect(() => setQuantity(1), [slug]);

	if (isPending) return <p className='page py-16 text-muted'>Loading product...</p>;
	if (error?.status === 404) return <Navigate to='/' replace />;
	if (error) return <p className='page py-16 text-red-600'>This product could not be loaded ({error.message})</p>;

	const isSoldOut = !product.inStock;
	const category = findCategory(product.category);

	const addAndResetQuantity = async () => {
		if (await addToCart(product.id, quantity)) setQuantity(1);
	};

	return (
		<div className='page flex flex-col gap-20 pb-24 pt-10'>
			<div className='flex flex-col gap-8'>
				<Link to={`/${product.category}`} className='label flex items-center gap-2 text-muted hover:text-brand-dark'>
					<ArrowLeft size={16} /> Back to {category?.label}
				</Link>

				<section className='grid grid-cols-1 items-center gap-10 md:grid-cols-2 lg:gap-24'>
					{/* Keyed by product so moving to another product starts again at its cover */}
					<ProductGallery key={product.slug} images={product.images} name={product.name} />
					<div className='flex flex-col gap-5'>
						{product.isNew && <p className='eyebrow'>New product</p>}
						<h1 className='text-[32px] leading-tight tracking-tight lg:text-[40px]'>{product.name}</h1>
						<p className='text-[15px] leading-relaxed text-muted'>{product.description}</p>
						<p className='flex items-baseline gap-4'>
							<span className='text-[24px] font-bold'>{formatPrice(product.price)}</span>
							{isFewLeft(product) && <span className='text-[13px] font-bold text-brand-dark'>{stockLabel(product)}</span>}
						</p>
						<div className='flex gap-3'>
							<QuantityStepper value={quantity} onChange={setQuantity} max={Math.max(1, product.stock)} label={product.name} />
							<button onClick={addAndResetQuantity} disabled={isSoldOut || busy} className='btn-primary'>
								<ShoppingBag size={18} />
								{busy ? 'Adding...' : isSoldOut ? 'Out of stock' : 'Add to cart'}
							</button>
						</div>
						{notice && <p className={`text-[14px] font-bold ${notice.ok ? 'text-brand-dark' : 'text-red-600'}`}>{notice.text}</p>}
					</div>
				</section>
			</div>

			<section className='grid grid-cols-1 gap-12 lg:grid-cols-3'>
				<div className='flex flex-col gap-6 lg:col-span-2'>
					<h2 className='text-[24px] md:text-[32px]'>Features</h2>
					<p className='whitespace-pre-line text-[15px] leading-relaxed text-muted'>{product.features}</p>
				</div>
				<div className='flex flex-col gap-6'>
					<h2 className='text-[24px] md:text-[32px]'>In the box</h2>
					<ul className='flex flex-col gap-2'>
						{product.includes.map((entry) => (
							<li key={entry.item} className='flex gap-6 text-[15px]'>
								<span className='w-6 font-bold text-brand'>{entry.quantity}x</span>
								<span className='text-muted'>{entry.item}</span>
							</li>
						))}
					</ul>
				</div>
			</section>

			<section className='flex flex-col gap-10'>
				<h2 className='text-center text-[24px] md:text-[32px]'>You may also like</h2>
				<div className='grid grid-cols-1 gap-6 md:grid-cols-3'>
					{relatedProducts.map((relatedProduct) => (
						<ProductCard key={relatedProduct.slug} product={relatedProduct} />
					))}
				</div>
			</section>
		</div>
	);
};

export default Product;
