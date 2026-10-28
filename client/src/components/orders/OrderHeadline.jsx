import { CircleCheck, CircleX, Loader } from 'lucide-react';

// The big message on top of an order: cancelled, waiting for the card payment, or thank you
const OrderHeadline = ({ order, paymentResult, isWaitingForPayment, hasStoppedWaiting, onPayAgain, isOpeningPaymentPage }) => {
	if (order.status === 'Cancelled') {
		return (
			<Message icon={CircleX} tone='text-red-600' title='Order cancelled'>
				Its items went back into stock.
				{order.payment?.status === 'Succeeded'
					? ' Your payment arrived after the cancellation, so it will be refunded.'
					: ' Nothing was charged.'}
			</Message>
		);
	}

	if (isWaitingForPayment) {
		const hasPaymentFailed = paymentResult === 'fail';
		const paymentPageUrl = order.payment?.checkoutUrl;
		return (
			<Message
				icon={hasPaymentFailed ? CircleX : Loader}
				tone={hasPaymentFailed ? 'text-red-600' : 'text-brand animate-spin'}
				title={hasPaymentFailed ? 'Your payment did not go through' : 'Confirming your payment'}
			>
				{hasPaymentFailed
					? 'Nothing was charged. Your items stay reserved for 30 minutes, so you can try again.'
					: hasStoppedWaiting
					? 'Fawaterak has not confirmed this payment yet. If you paid, refresh this page in a minute.'
					: 'We are waiting for Fawaterak to confirm the payment. This page updates by itself.'}
				{/* A pending result keeps its page, it may hold a code to pay at a kiosk, anything else gets a new page */}
				{paymentResult === 'pending' && paymentPageUrl && (
					<a href={paymentPageUrl} className='btn-primary mt-4'>
						Go to the payment page
					</a>
				)}
				{paymentResult !== 'pending' && paymentResult !== 'success' && (
					<button onClick={onPayAgain} disabled={isOpeningPaymentPage} className='btn-primary mt-4'>
						{isOpeningPaymentPage
							? 'Opening the payment page...'
							: hasPaymentFailed
							? 'Try paying again'
							: 'Go to the payment page'}
					</button>
				)}
			</Message>
		);
	}

	return (
		<Message icon={CircleCheck} tone='text-brand' title='Thank you for your order'>
			{order.paymentMethod === 'Card'
				? 'Your payment is confirmed. We will start preparing your order.'
				: 'Pay in cash when the courier arrives. We will start preparing your order.'}
		</Message>
	);
};

const Message = ({ icon: Icon, tone, title, children }) => (
	<div className='flex gap-4'>
		<Icon size={32} className={`shrink-0 ${tone}`} />
		<div className='flex flex-col items-start gap-1'>
			<h1 className='text-[24px] md:text-[28px]'>{title}</h1>
			<div className='flex flex-col items-start text-[15px] text-muted'>{children}</div>
		</div>
	</div>
);

export default OrderHeadline;
