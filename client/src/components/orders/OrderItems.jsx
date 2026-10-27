import { formatPrice } from '../../lib/format';

// The products of an order as small rows with picture, name, quantity and line total
const OrderItems = ({ items }) => (
	<ul className='flex flex-col gap-3'>
		{items.map((item) => (
			<li key={item.productId} className='flex items-center justify-between gap-4 rounded bg-panel p-3'>
				<div className='flex min-w-0 items-center gap-4'>
					{item.productImageUrl && <img src={item.productImageUrl} alt='' className='h-14 w-14 rounded bg-surface' />}
					<div className='min-w-0'>
						<p className='truncate text-[15px] font-bold'>{item.productName}</p>
						<p className='text-[13px] text-muted'>
							{formatPrice(item.unitPrice)} x {item.quantity}
						</p>
					</div>
				</div>
				<p className='shrink-0 text-[16px] font-bold'>{formatPrice(item.lineTotal)}</p>
			</li>
		))}
	</ul>
);

export default OrderItems;
