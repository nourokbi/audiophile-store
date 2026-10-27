import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ArrowLeft, ArrowRight, Lock, ShoppingBag } from 'lucide-react';

import AddressForm from '../components/checkout/AddressForm';
import AddressPicker from '../components/checkout/AddressPicker';
import PaymentPicker from '../components/checkout/PaymentPicker';
import PriceSummary from '../components/shop/PriceSummary';
import EmptyState from '../components/ui/EmptyState';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { useAddresses } from '../hooks/useAddresses';
import { ordersApi } from '../lib/api/orders';
import { formatPrice } from '../lib/format';

const NEW_ADDRESS_ID = 'new';
const EMPTY_ADDRESS_FORM = { name: '', phone: '', street: '', postalCode: '', city: '', country: 'Egypt' };

function findAddressErrors(form) {
	const fieldErrors = {};
	if (form.name.trim().length < 3) fieldErrors.name = 'Required';
	if (!/^\+?[\d\s()-]{7,20}$/.test(form.phone.trim())) fieldErrors.phone = 'Invalid phone';
	if (form.street.trim().length < 3) fieldErrors.street = 'Required';
	if (form.city.trim().length < 2) fieldErrors.city = 'Required';
	if (form.country.trim().length < 2) fieldErrors.country = 'Required';
	return fieldErrors;
}

// Pick a saved address or add one, place the order from the cart, then card goes to Fawaterak and cash to the order page
const Checkout = () => {
	const { user } = useAuth();
	const { cart, refresh: refreshCart } = useCart();
	const navigate = useNavigate();
	const { addresses, addAddress } = useAddresses();

	const [chosenAddressId, setChosenAddressId] = React.useState(null); // null until the customer picks one
	const [addressForm, setAddressForm] = React.useState(EMPTY_ADDRESS_FORM);
	const [paymentMethod, setPaymentMethod] = React.useState('Card');
	const [fieldErrors, setFieldErrors] = React.useState({});
	const [serverError, setServerError] = React.useState('');
	const [isPlacingOrder, setIsPlacingOrder] = React.useState(false);

	// One key per checkout attempt, so a double click or a retry returns the same order
	const idempotencyKey = React.useRef(crypto.randomUUID());

	// The newest saved address is picked first, with none saved the new address form shows straight away
	const selectedAddressId = chosenAddressId ?? addresses?.at(-1)?.id ?? NEW_ADDRESS_ID;
	const isNewAddress = selectedAddressId === NEW_ADDRESS_ID;

	const updateAddressField = (event) => setAddressForm({ ...addressForm, [event.target.name]: event.target.value });

	// Saves the new address once and selects it, so a retry after an error does not save it again
	const saveNewAddress = async () => {
		const createdAddress = await addAddress({
			fullAddressName: addressForm.name.trim(),
			street: addressForm.street.trim(),
			city: addressForm.city.trim(),
			country: addressForm.country.trim(),
			phone: addressForm.phone.trim(),
			postalCode: addressForm.postalCode.trim() || null,
		});
		setChosenAddressId(createdAddress.id);
		return createdAddress.id;
	};

	const placeOrder = async (event) => {
		event.preventDefault();
		setServerError('');
		if (isNewAddress) {
			const addressErrors = findAddressErrors(addressForm);
			setFieldErrors(addressErrors);
			if (Object.keys(addressErrors).length > 0) return;
		}

		setIsPlacingOrder(true);
		try {
			const addressId = isNewAddress ? await saveNewAddress() : selectedAddressId;
			const order = await ordersApi.place(addressId, paymentMethod, idempotencyKey.current);

			if (order.paymentMethod === 'Card' && order.payment?.checkoutUrl) {
				window.location.assign(order.payment.checkoutUrl);
				return;
			}

			idempotencyKey.current = crypto.randomUUID();
			await refreshCart();
			navigate(`/orders/${order.id}`);
		} catch (error) {
			setServerError(error.message);
			setIsPlacingOrder(false);
		}
	};

	if (cart.items.length === 0 && !isPlacingOrder) {
		return (
			<div className='page py-16'>
				<EmptyState icon={ShoppingBag} title='Your cart is empty'>
					<Link to='/' className='btn-primary'>
						Continue shopping
					</Link>
					<Link to='/orders' className='btn-secondary'>
						My orders
					</Link>
				</EmptyState>
			</div>
		);
	}

	return (
		<form onSubmit={placeOrder} noValidate className='page flex flex-col gap-8 pb-24 pt-10'>
			<div className='flex flex-wrap items-end justify-between gap-4'>
				<div>
					<Link to='/cart' className='label flex items-center gap-2 text-muted hover:text-brand-dark'>
						<ArrowLeft size={16} /> Back to cart
					</Link>
					<h1 className='title mt-4'>Checkout</h1>
				</div>
				<p className='label flex items-center gap-2 rounded bg-panel px-3 py-2 text-[11px]'>
					<Lock size={14} /> Stock is reserved when you place the order
				</p>
			</div>

			<div className='grid grid-cols-1 items-start gap-8 lg:grid-cols-3'>
				<div className='flex flex-col gap-6 lg:col-span-2'>
					<CheckoutStep number='01' title='Shipping address'>
						{addresses === null && <p className='text-muted'>Loading your addresses...</p>}
						{addresses?.length > 0 && (
							<AddressPicker
								addresses={addresses}
								selectedAddressId={selectedAddressId}
								newAddressId={NEW_ADDRESS_ID}
								onSelect={setChosenAddressId}
							/>
						)}
						{addresses !== null && isNewAddress && (
							<AddressForm form={addressForm} fieldErrors={fieldErrors} onFieldChange={updateAddressField} />
						)}
					</CheckoutStep>

					<CheckoutStep number='02' title='Payment method'>
						<PaymentPicker paymentMethod={paymentMethod} onSelect={setPaymentMethod} />
					</CheckoutStep>
				</div>

				<aside className='card flex flex-col gap-5 p-6 lg:sticky lg:top-28'>
					<div className='flex items-center justify-between'>
						<h2 className='text-[18px]'>Summary</h2>
						<span className='rounded bg-panel px-2 py-1 text-[11px] font-bold uppercase'>
							{cart.totalQuantity} {cart.totalQuantity === 1 ? 'item' : 'items'}
						</span>
					</div>
					<ul className='flex max-h-[280px] flex-col gap-4 overflow-y-auto'>
						{cart.items.map((item) => (
							<li key={item.productId} className='flex items-center justify-between gap-3'>
								<div className='flex min-w-0 items-center gap-3'>
									{item.imageUrl && <img src={item.imageUrl} alt='' className='h-12 w-12 rounded bg-panel object-cover' />}
									<div className='min-w-0'>
										<p className='truncate text-[14px] font-bold'>{item.productName}</p>
										<p className='text-[13px] text-muted'>{formatPrice(item.unitPrice)}</p>
									</div>
								</div>
								<span className='text-[14px] font-bold text-muted'>x{item.quantity}</span>
							</li>
						))}
					</ul>
					<PriceSummary subTotal={cart.subTotal} shippingFee={cart.shippingFee} total={cart.total} totalLabel='Grand total' />
					<p className='text-[13px] text-muted'>The confirmation goes to {user?.email}</p>
					{serverError && (
						<p role='alert' className='error-text'>
							{serverError}
						</p>
					)}
					<button type='submit' disabled={isPlacingOrder || addresses === null} className='btn-primary w-full'>
						{isPlacingOrder
							? 'Placing your order...'
							: paymentMethod === 'Card'
							? `Continue and pay ${formatPrice(cart.total)}`
							: 'Place order'}
						{!isPlacingOrder && <ArrowRight size={16} />}
					</button>
				</aside>
			</div>
		</form>
	);
};

const CheckoutStep = ({ number, title, children }) => (
	<section className='card flex flex-col gap-5 p-6 md:p-8'>
		<h2 className='flex items-center gap-3 text-[18px]'>
			<span className='rounded bg-panel px-2 py-1 text-[12px] text-muted'>{number}</span>
			{title}
		</h2>
		{children}
	</section>
);

export default Checkout;
