import React from 'react';

import Menu from '../components/Menu';
import BestGear from '../components/BestGear';

import desktopZX9 from '/assets/home/desktop/image-speaker-zx9.png';
import tabletZX9 from '/assets/home/tablet/image-speaker-zx9.png';
import mobileZX9 from '/assets/home/mobile/image-speaker-zx9.png';
import desktopZX7 from '/assets/home/desktop/image-speaker-zx7.jpg';
import tabletZX7 from '/assets/home/tablet/image-speaker-zx7.jpg';
import mobileZX7 from '/assets/home/mobile/image-speaker-zx7.jpg';
import { Link } from 'react-router-dom';

const Home = () => {
	return (
		<main>
			<header className='xs:bg-xs-hero-pattern md:bg-md-hero-pattern mt-[95px] lg:bg-lg-hero-pattern bg-no-repeat bg-center bg-cover md:px-[40px] lg:px-[165px] w-full'>
				<div className='border-t-[1px] border-white/40 border-solid pt-[128px] pb-[158px] flex flex-col justify-center xs:items-center xs:text-center lg:items-start lg:text-left'>
					<div className='w-[398px] h-[346px]'>
						<p className='text-white/50 font-normal uppercase tracking-[10px]'>
							New Product
						</p>
						<h1 className='xs:text-[36px] xs:leading-[40px] xs:tracking-[1.3px] md:text-[56px] md:leading-[58px] md:tracking-[2px] text-white mt-6'>
							XX99 Mark II Headphones
						</h1>
						<p className='mt-6 text-white/75 xs:px-[20px] lg:px-0 lg:w-[360px]'>
							Experience natural, lifelike audio and exceptional build quality
							made for the passionate music enthusiast.
						</p>
						<Link to='headphones/xx99-mark-two-headphones'>
							<button className='btn bg-cream hover:bg-cream-light transition-all duration-150 mt-10'>
								See Product
							</button>
						</Link>
					</div>
				</div>
			</header>

			<section className='xs:px-[24px] md:px-[40px] lg:px-[165px] md:mt-[52px] lg:mt-[92px] z-[0]'>
				<Menu />
			</section>

			<section className='xs:px-[24px] md:px-[40px] lg:px-[165px] h-full flex flex-col xs:gap-6 md:gap-8 lg:gap-12 '>
				<div className='bg-cream bg-circles bg-no-repeat xs:bg-bottom lg:bg-top flex xs:flex-col lg:flex-row items-center justify-evenly md:h-[720px] lg:h-[560px] xs:py-14 md:py-16 lg:py-0 lg:min-w-[730px] rounded-[8px] z-[-1]'>
					<div className='h-full lg:overflow-hidden '>
						{/* desktop img */}
						<img
							src={desktopZX9}
							className='xs:hidden lg:block relative translate-y-[24%] w-[380px]'
							alt='ZX9 Speaker'
						/>
						{/* tablet img */}
						<img
							src={tabletZX9}
							className='xs:hidden md:block lg:hidden h-[237px]'
							alt='ZX9 Speaker'
						/>
						{/* mobile img */}
						<img
							src={mobileZX9}
							className='xs:block md:hidden h-[207px]'
							alt='ZX9 Speaker'
						/>
					</div>
					<div className='w-[350px] flex flex-col xs:items-center lg:items-start xs:px-6 md:px-0 '>
						<h1 className='xs:text-[36px] md:text-[56px] font-bold xs:leading-[40px] md:leading-[58px] xs:tracking-[1.3px] md:tracking-[2px] uppercase xs:mt-8 md:mt-16 lg:mt-0 text-white xs:text-center lg:text-left xs:px-14 md:px-0'>
							ZX9 Speaker
						</h1>
						<p className='p text-white mt-6 xs:text-center lg:text-left'>
							Upgrade to premium speakers that are phenomenally built to deliver
							truly remarkable sound.
						</p>
						<Link to='speakers/zx9-speaker'>
							<button className='btn bg-black border border-black hover:bg-white hover:text-black transition-all duration-150 xs:mt-6 md:mt-10'>
								See Product
							</button>
						</Link>
					</div>
				</div>

				<div className='relative lg:min-w-[730px] z-[-1]'>
					{/* desktop img */}
					<img
						src={desktopZX7}
						className='xs:hidden lg:block w-full rounded-[8px]'
					/>
					{/* tablet img */}
					<img
						src={tabletZX7}
						className='xs:hidden md:block lg:hidden w-full rounded-[8px]'
					/>
					{/* mobile img */}
					<img
						src={mobileZX7}
						className='xs:block md:hidden w-full rounded-[8px]'
					/>
					<div className='absolute top-0 h-full flex flex-col items-start xs:justify-center md:justify-evenly py-[100px] xs:px-6 md:px-12 lg:px-20 xs:gap-8 md:gap-4'>
						<h4 className='h4'>ZX7 Speaker</h4>
						<Link to='speakers/zx7-speaker'>
							<button className='btn bg-transparent text-black border-black border hover:bg-black hover:text-white transition-all duration-150'>
								See Product
							</button>
						</Link>
					</div>
				</div>

				<div className='flex xs:flex-col md:flex-row xs:gap-6 md:gap-[11px] lg:gap-[30px] w-full xs:h-[424px] md:h-full lg:min-w-[730px]'>
					{/* desktop img */}
					<div className='xs:hidden lg:block flex-1 bg-lg-YX1 h-[360px] bg-no-repeat bg-cover bg-center rounded-[8px]'></div>
					{/* tablet img */}
					<div className='xs:hidden md:block lg:hidden flex-1 bg-md-YX1 h-[320px] bg-no-repeat bg-cover bg-center rounded-[8px]'></div>
					{/* mobile img */}
					<div className='xs:block md:hidden flex-1 bg-xs-YX1 bg-no-repeat bg-cover bg-center rounded-[8px]'></div>
					<div className='flex-1 bg-gray-light xs:h-[400px] md:h-[320px] lg:h-[360px] rounded-[8px]'>
						<div className='h-full flex flex-col items-start xs:justify-center md:justify-evenly md:py-[100px] xs:px-6 md:px-10 lg:px-20 xs:gap-8 md:gap-4'>
							<h4 className='h4'>YX1 Earphones</h4>
							<Link to='earphones/yx1-earphones'>
								<button className='btn bg-transparent text-black border-black border hover:bg-black hover:text-white transition-all duration-150'>
									See Product
								</button>
							</Link>
						</div>
					</div>
				</div>
			</section>

			<BestGear />
		</main>
	);
};

export default Home;
