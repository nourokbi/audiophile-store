import React from 'react';
import { Link, useNavigate } from 'react-router-dom';

import CoDLogo from '/assets/checkout/icon-cash-on-delivery.svg';
import TextField from '../components/TextField';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { api } from '../lib/api';
import { formatPrice } from '../lib/format';

// Checkout: save the shipping address, place the order from the server cart, then
//   Card           → go to Fawaterak's payment page (payment.checkoutUrl)
//   CashOnDelivery → straight to the order page, already confirmed.
// The totals shown are the API's (GET /cart), the same numbers the order will use.
const EMPTY_FORM = { name: '', phoneNumber: '', address: '', zipCode: '', city: '', country: '' };

const Checkout = () => {
	const { user } = useAuth();
	const { cart, refresh } = useCart();
	const navigate = useNavigate();

	const [method, setMethod] = React.useState('Card');
	const [form, setForm] = React.useState(EMPTY_FORM);
	const [saved, setSaved] = React.useState(null); // the address used last time, if any
	const [errors, setErrors] = React.useState({});
	const [serverError, setServerError] = React.useState('');
	const [busy, setBusy] = React.useState(false);

	// One key per checkout attempt: pressing the button twice, or retrying after a network
	// error, returns the same order instead of creating a second one (Idempotency-Key).
	const idempotencyKey = React.useRef(crypto.randomUUID());

	// Fill the form with the customer's most recent saved address.
	React.useEffect(() => {
		api('/addresses')
			.then((list) => {
				const last = list.at(-1);
				if (!last) return;
				setSaved(last);
				setForm({
					name: last.fullAddressName,
					phoneNumber: last.phone,
					address: last.street,
					zipCode: last.postalCode ?? '',
					city: last.city,
					country: last.country,
				});
			})
			.catch(() => {});
	}, []);

	const onChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

	const validate = () => {
		const next = {};
		if (form.name.trim().length < 3) next.name = 'Name is required';
		if (!/^\+?[\d\s()-]{7,20}$/.test(form.phoneNumber.trim())) next.phoneNumber = 'Invalid phone number';
		if (form.address.trim().length < 3) next.address = 'Address is required';
		if (form.city.trim().length < 2) next.city = 'City is required';
		if (form.country.trim().length < 2) next.country = 'Country is required';
		setErrors(next);
		return Object.keys(next).length === 0;
	};

	// Reuse the saved address when nothing changed; otherwise save the new one.
	const addressId = async () => {
		const body = {
			fullAddressName: form.name.trim(),
			street: form.address.trim(),
			city: form.city.trim(),
			country: form.country.trim(),
			phone: form.phoneNumber.trim(),
			postalCode: form.zipCode.trim() || null,
		};
		const same =
			saved &&
			saved.fullAddressName === body.fullAddressName &&
			saved.street === body.street &&
			saved.city === body.city &&
			saved.country === body.country &&
			saved.phone === body.phone &&
			(saved.postalCode ?? null) === body.postalCode;
		if (same) return saved.id;

		const created = await api('/addresses', { method: 'POST', body });
		setSaved(created);
		return created.id;
	};

	const onSubmit = async (e) => {
		e.preventDefault();
		setServerError('');
		if (!validate()) return;

		setBusy(true);
		try {
			const order = await api('/orders', {
				method: 'POST',
				headers: { 'Idempotency-Key': idempotencyKey.current },
				body: { addressId: await addressId(), paymentMethod: method },
			});

			if (order.paymentMethod === 'Card' && order.payment?.checkoutUrl) {
				// Leave the shop for Fawaterak's hosted page. It sends the browser back to
				// /payments/fawaterak/return, which forwards to /orders/:id.
				window.location.assign(order.payment.checkoutUrl);
				return;
			}

			idempotencyKey.current = crypto.randomUUID();
			await refresh(); // the order emptied the cart on the server
			navigate(`/orders/${order.id}`);
		} catch (err) {
			setServerError(err.message);
			setBusy(false);
		}
	};

	if (cart.items.length === 0 && !busy) {
		return (
			<main className='mt-[95px] bg-gray-light'>
				<div className='xs:px-[24px] md:px-[40px] lg:px-[165px] xs:pt-10 md:pt-16 lg:pt-[79px] pb-[140px]'>
					<section className='bg-white xs:p-6 md:p-12 rounded-[8px]'>
						<h3>Checkout</h3>
						<p className='p text-black/50 mt-4'>Your cart is empty.</p>
						<div className='flex gap-4 mt-8'>
							<Link to='/'>
								<button className='btn bg-cream hover:bg-cream-light'>Continue shopping</button>
							</Link>
							<Link to='/orders'>
								<button className='btn bg-stone hover:bg-black/70'>My orders</button>
							</Link>
						</div>
					</section>
				</div>
			</main>
		);
	}

	return (
		<main className='mt-[95px] bg-gray-light'>
			<div className='xs:px-[24px] md:px-[40px] lg:px-[165px] xs:pt-4 md:pt-8 lg:pt-[79px]'>
				<button
					onClick={() => navigate(-1)}
					className='p text-black/50 cursor-pointer hover:text-cream transition-all duration-150'
				>
					Go back
				</button>
			</div>
			<form
				onSubmit={onSubmit}
				noValidate
				className='xs:px-[24px] md:px-[40px] lg:px-[165px] mt-[38px] pb-[140px] flex xs:flex-col xl:flex-row gap-8 xs:items-center xl:items-start'
			>
				<section className='bg-white xs:p-6 md:p-12 rounded-[8px] w-full'>
					<h3>Checkout</h3>

					<div className='mt-10'>
						<p className='text-cream text-[13px] font-bold uppercase leading-[25px] tracking-wide'>Billing Details</p>
						<div className='mt-4 flex flex-col gap-6'>
							<div className='flex xs:flex-col md:flex-row items-center gap-4'>
								<TextField
									label='Name'
									name='name'
									autoComplete='name'
									placeholder='Alexei Ward'
									value={form.name}
									onChange={onChange}
									error={errors.name}
								/>
								<TextField label='Email address' name='email' value={user?.email ?? ''} disabled readOnly />
							</div>
							<TextField
								label='Phone number'
								name='phoneNumber'
								type='tel'
								autoComplete='tel'
								placeholder='+20 100 000 0000'
								value={form.phoneNumber}
								onChange={onChange}
								error={errors.phoneNumber}
							/>
						</div>
					</div>

					<div className='mt-[53px]'>
						<p className='text-cream text-[13px] font-bold uppercase leading-[25px] tracking-wide'>Shipping Info</p>
						<div className='mt-4 flex flex-col gap-6'>
							<TextField
								label='Address'
								name='address'
								autoComplete='street-address'
								placeholder='12 Tahrir Street'
								value={form.address}
								onChange={onChange}
								error={errors.address}
							/>
							<div className='flex xs:flex-col md:flex-row items-center gap-4'>
								<TextField
									label='ZIP Code (optional)'
									name='zipCode'
									autoComplete='postal-code'
									placeholder='11511'
									maxLength={20}
									value={form.zipCode}
									onChange={onChange}
								/>
								<TextField
									label='City'
									name='city'
									autoComplete='address-level2'
									placeholder='Cairo'
									value={form.city}
									onChange={onChange}
									error={errors.city}
								/>
							</div>
							<TextField
								label='Country'
								name='country'
								autoComplete='country-name'
								placeholder='Egypt'
								value={form.country}
								onChange={onChange}
								error={errors.country}
							/>
						</div>
					</div>

					<div className='mt-[61px]'>
						<p className='text-cream text-[13px] font-bold uppercase leading-[25px] tracking-wide'>Payment Details</p>
						<div className='flex xs:flex-col xs:gap-[17px] md:gap-0 md:flex-row mt-4 justify-between'>
							<p className='text-black text-xs font-bold flex-1'>Payment Method</p>
							<div className='flex flex-col flex-1 gap-4'>
								{[
									['Card', 'Card (Fawaterak)'],
									['CashOnDelivery', 'Cash on Delivery'],
								].map(([value, label]) => (
									<label
										key={value}
										className={`flex items-center gap-4 p-[18px] rounded-lg border cursor-pointer hover:border-cream transition-all duration-150 ${
											method === value ? 'border-cream' : 'border-stone-300'
										}`}
									>
										<input
											type='radio'
											name='payment'
											value={value}
											checked={method === value}
											onChange={() => setMethod(value)}
											className='cursor-pointer accent-cream'
										/>
										<span className='text-black text-xs font-bold'>{label}</span>
									</label>
								))}
							</div>
						</div>

						<div className='mt-[30px] flex xs:flex-col md:flex-row items-center gap-8'>
							{method === 'CashOnDelivery' && <img src={CoDLogo} alt='' />}
							<p className='text-black/50 text-[15px] font-medium leading-[25px]'>
								{method === 'Card'
									? 'You’ll enter your card on Fawaterak’s secure payment page. Your card details never reach our servers. Staging test card: 5123 4500 0000 0008, 12/26, CVV 100.'
									: 'The ‘Cash on Delivery’ option enables you to pay in cash when our delivery courier arrives at your residence. Just make sure your address is correct so that your order will not be cancelled.'}
							</p>
						</div>
					</div>
				</section>

				<section className='rounded-[8px] p-8 bg-white xs:w-full xl:w-auto'>
					<h6 className='h6 text-black'>Summary</h6>
					<div className='flex flex-col items-center gap-6 py-8 overflow-y-auto max-h-[304px] xs:w-full lg:w-[284px]'>
						{cart.items.map((item) => (
							<div key={item.productId} className='w-full flex items-center justify-between'>
								<div className='flex items-center gap-4'>
									{item.imageUrl && <img src={item.imageUrl} alt='' className='w-16 h-16 rounded-[8px]' />}
									<div className='flex flex-col'>
										<p className='text-[15px] font-bold leading-[25px] text-black'>{item.productName}</p>
										<p className='text-[14px] font-bold leading-[25px] text-black/50'>{formatPrice(item.unitPrice)}</p>
									</div>
								</div>
								<p className='text-[15px] font-bold text-black/50 p-[15px]'>x{item.quantity}</p>
							</div>
						))}
					</div>

					<div className='flex flex-col gap-2'>
						<SummaryRow label='Total' value={formatPrice(cart.subTotal)} />
						<SummaryRow label='Shipping' value={cart.shippingFee > 0 ? formatPrice(cart.shippingFee) : 'Free'} />
						<div className='flex items-center justify-between mt-4'>
							<p className='text-[15px] font-[500] leading-[25px] text-black/50 uppercase'>Grand Total</p>
							<h6 className='font-bold leading-[25px] text-cream'>{formatPrice(cart.total)}</h6>
						</div>
					</div>

					{serverError && (
						<p role='alert' className='text-red-600 text-[14px] font-bold mt-6 lg:max-w-[284px]'>
							{serverError}
						</p>
					)}

					<button
						type='submit'
						disabled={busy}
						className='btn w-full bg-cream text-white hover:bg-cream-light transition-all duration-150 mt-8 disabled:opacity-60 disabled:cursor-wait'
					>
						{busy ? 'Placing your order…' : method === 'Card' ? 'Continue & Pay' : 'Place order'}
					</button>
				</section>
			</form>
		</main>
	);
};

const SummaryRow = ({ label, value }) => (
	<div className='flex items-center justify-between'>
		<p className='text-[15px] font-[500] leading-[25px] text-black/50 uppercase'>{label}</p>
		<h6 className='font-bold leading-[25px] text-black'>{value}</h6>
	</div>
);

export default Checkout;
