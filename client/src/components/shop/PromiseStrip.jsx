import { BadgeCheck, CreditCard, PackageX, Truck } from 'lucide-react';

import { formatPrice } from '../../lib/format';
import { SHIPPING } from '../../lib/shipping';

const PROMISES = [
	{ icon: Truck, title: 'Free shipping', text: `On every order of ${formatPrice(SHIPPING.freeFrom)} or more` },
	{ icon: CreditCard, title: 'Secure payment', text: 'Card details stay on Fawaterak, never on our servers' },
	{ icon: PackageX, title: 'Easy cancel', text: 'Cancel any order before it ships' },
	{ icon: BadgeCheck, title: 'Real stock', text: 'What you see is what we have, reserved when you order' },
];

// The four shop promises under the product grid
const PromiseStrip = () => (
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
);

export default PromiseStrip;
