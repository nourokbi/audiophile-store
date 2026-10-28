import React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

import Picture from '../ui/Picture';

// The product's photos, one large with arrows to move through them and a row of thumbnails to jump to one
const ProductGallery = ({ images, name }) => {
	const [shownIndex, setShownIndex] = React.useState(0);
	const hasMany = images.length > 1;

	// The arrows go round, so the last photo leads back to the first
	const showNext = (step) => setShownIndex((index) => (index + step + images.length) % images.length);

	return (
		<div className='flex flex-col gap-4'>
			<div className='relative overflow-hidden rounded bg-panel shadow-sm'>
				<Picture
					image={images[shownIndex]}
					alt={hasMany ? `${name}, photo ${shownIndex + 1} of ${images.length}` : name}
					className='aspect-square w-full object-cover'
				/>
				{hasMany && (
					<>
						<button
							type='button'
							onClick={() => showNext(-1)}
							aria-label='Previous photo'
							className='absolute left-3 top-1/2 -translate-y-1/2 rounded-full bg-surface/80 p-2 shadow hover:bg-surface'
						>
							<ChevronLeft size={20} />
						</button>
						<button
							type='button'
							onClick={() => showNext(1)}
							aria-label='Next photo'
							className='absolute right-3 top-1/2 -translate-y-1/2 rounded-full bg-surface/80 p-2 shadow hover:bg-surface'
						>
							<ChevronRight size={20} />
						</button>
					</>
				)}
			</div>

			{hasMany && (
				<div className='flex gap-3 overflow-x-auto pb-1'>
					{images.map((image, index) => (
						<button
							key={image.desktop}
							type='button'
							onClick={() => setShownIndex(index)}
							aria-label={`Show photo ${index + 1}`}
							aria-current={index === shownIndex}
							className={`w-20 shrink-0 overflow-hidden rounded border-2 bg-panel ${
								index === shownIndex ? 'border-brand' : 'border-transparent opacity-70 hover:opacity-100'
							}`}
						>
							<img src={image.mobile} alt='' loading='lazy' className='aspect-square w-full object-cover' />
						</button>
					))}
				</div>
			)}
		</div>
	);
};

export default ProductGallery;
