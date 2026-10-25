import { formatPrice } from '../../lib/format';

// The grey box with the subtotal, the shipping fee and the total, used by the cart, checkout and an order
const PriceSummary = ({ subTotal, shippingFee, total, subTotalLabel = 'Subtotal', totalLabel = 'Total' }) => (
	<div className='flex flex-col gap-2 rounded bg-panel p-4 text-[15px]'>
		<SummaryRow label={subTotalLabel} value={formatPrice(subTotal)} />
		<SummaryRow label='Shipping' value={shippingFee > 0 ? formatPrice(shippingFee) : 'Free'} />
		<div className='mt-2 flex items-center justify-between border-t border-line pt-3'>
			<span className='label'>{totalLabel}</span>
			<span className='text-[24px] font-bold text-brand'>{formatPrice(total)}</span>
		</div>
	</div>
);

const SummaryRow = ({ label, value }) => (
	<div className='flex items-center justify-between'>
		<span className='text-muted'>{label}</span>
		<span className='font-bold'>{value}</span>
	</div>
);

export default PriceSummary;
