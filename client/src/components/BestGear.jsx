import React from 'react';

import desktopBestGear from '/assets/shared/desktop/image-best-gear.jpg';
import tabletBestGear from '/assets/shared/tablet/image-best-gear.jpg';
import mobileBestGear from '/assets/shared/mobile/image-best-gear.jpg';

const BestGear = () => {
	return (
		<section className='xs:px-[24px] md:px-[40px] lg:px-[165px] xs:py-[120px] md:py-[96px] lg:py-[200px]'>
			<div className='lg:min-w-[730px] flex xs:flex-col-reverse lg:flex-row items-center justify-between xs:gap-10 md:gap-16 lg:gap-0'>
				<div className='xs:text-center flex md:w-[580px] lg:w-[450px] flex-col gap-8'>
					<h2 className='xs:text-[28px] md:text-[40px] font-bold xs:leading-normal md:leading-[44px] xs:tracking-[1px] md:tracking-[1.4px] uppercase;'>
						Bringing you the <span className='text-cream'>best</span> audio gear
					</h2>
					<p className='p text-black/50'>
						Located at the heart of New York City, Audiophile is the premier
						store for high end headphones, earphones, speakers, and audio
						accessories. We have a large showroom and luxury demonstration rooms
						available for you to browse and experience a wide range of our
						products. Stop by our store to meet some of the fantastic people who
						make Audiophile the best place to buy your portable audio equipment.
					</p>
				</div>
				<div className='lg:min-h-full'>
					{/* desktop image */}
					<img
						src={desktopBestGear}
						className='xs:hidden lg:block rounded-[8px]'
					/>
					{/* tablet image */}
					<img
						src={tabletBestGear}
						className='xs:hidden md:block lg:hidden rounded-[8px]'
					/>
					{/* mobile image */}
					<img
						src={mobileBestGear}
						className='xs:block md:hidden rounded-[8px]'
					/>
				</div>
			</div>
		</section>
	);
};

export default BestGear;
