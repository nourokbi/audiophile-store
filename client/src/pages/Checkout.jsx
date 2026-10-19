import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ArrowLeft, ArrowRight, Banknote, Check, CreditCard, Lock, MapPin, Plus, ShieldCheck, ShoppingBag } from 'lucide-react';

import TextField from '../components/TextField';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { api } from '../lib/api';
import { formatPrice } from '../lib/format';

const NEW_ADDRESS = 'new';
const EMPTY_FORM = { name: '', phone: '', street: '', postalCode: '', city: '', country: 'Egypt' };

const METHODS = [
	{ value: 'Card', label: 'Card', note: 'Paid on Fawaterak', icon: CreditCard },
	{ value: 'CashOnDelivery', label: 'Cash on delivery', note: 'Pay the courier', icon: Banknote },
];

// Pick a saved address or add one, place the order from the cart, then card goes to Fawaterak and cash to the order page
const Checkout = () => {
	const { user } = useAuth();
	const { cart, refresh } = useCart();
	const navigate = useNavigate();

	const [addresses, setAddresses] = React.useState(null);
	const [selected, setSelected] = React.useState(NEW_ADDRESS);
	const [form, setForm] = React.useState(EMPTY_FORM);
	const [method, setMethod] = React.useState('Card');
	const [errors, setErrors] = React.useState({});
	const [serverError, setServerError] = React.useState('');
	const [busy, setBusy] = React.useState(false);

	// One key per checkout attempt, so a double click or a retry returns the same order
	const idempotencyKey = React.useRef(crypto.randomUUID());

	// The most recent saved address is picked first, with none saved the new address form shows straight away
	React.useEffect(() => {
		api('/addresses')
			.then((list) => {
				setAddresses(list);
				if (list.length > 0) setSelected(list.at(-1).id);
			})
			.catch(() => setAddresses([]));
	}, []);

	const onChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

	const validate = () => {
		const next = {};
		if (form.name.trim().length < 3) next.name = 'Required';
		if (!/^\+?[\d\s()-]{7,20}$/.test(form.phone.trim())) next.phone = 'Invalid phone';
		if (form.street.trim().length < 3) next.street = 'Required';
		if (form.city.trim().length < 2) next.city = 'Required';
		if (form.country.trim().length < 2) next.country = 'Required';
		setErrors(next);
		return Object.keys(next).length === 0;
	};

	// Saves the new address once and selects it, so a retry after an error does not save it again
	const saveNewAddress = async () => {
		const created = await api('/addresses', {
			method: 'POST',
			body: {
				fullAddressName: form.name.trim(),
				street: form.street.trim(),
				city: form.city.trim(),
				country: form.country.trim(),
				phone: form.phone.trim(),
				postalCode: form.postalCode.trim() || null,
			},
		});
		setAddresses((list) => [...list, created]);
		setSelected(created.id);
		return created.id;
	};

	const onSubmit = async (e) => {
		e.preventDefault();
		setServerError('');
		if (selected === NEW_ADDRESS && !validate()) return;

		setBusy(true);
		try {
			const addressId = selected === NEW_ADDRESS ? await saveNewAddress() : selected;
			const order = await api('/orders', {
				method: 'POST',
				headers: { 'Idempotency-Key': idempotencyKey.current },
				body: { addressId, paymentMethod: method },
			});

			if (order.paymentMethod === 'Card' && order.payment?.checkoutUrl) {
				window.location.assign(order.payment.checkoutUrl);
				return;
			}

			idempotencyKey.current = crypto.randomUUID();
			await refresh();
			navigate(`/orders/${order.id}`);
		} catch (err) {
			setServerError(err.message);
			setBusy(false);
		}
	};

	if (cart.items.length === 0 && !busy) {
		return (
			<div className='page py-16'>
				<div className='card flex flex-col items-center gap-4 p-10 text-center'>
					<ShoppingBag size={32} className='text-muted' />
					<h1 className='text-[24px]'>Your cart is empty</h1>
					<div className='flex gap-3'>
						<Link to='/' className='btn-primary'>
							Continue shopping
						</Link>
						<Link to='/orders' className='btn-secondary'>
							My orders
						</Link>
					</div>
				</div>
			</div>
		);
	}

	return (
		<form onSubmit={onSubmit} noValidate className='page flex flex-col gap-8 pb-24 pt-10'>
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
					<Section number='01' title='Shipping address'>
						{addresses === null && <p className='text-muted'>Loading your addresses...</p>}

						{addresses?.length > 0 && (
							<div className='grid grid-cols-1 gap-3 md:grid-cols-2'>
								{addresses.map((a) => (
									<Choice key={a.id} active={selected === a.id} onClick={() => setSelected(a.id)}>
										<span className='flex items-center gap-2 font-bold'>
											<MapPin size={16} className='text-brand' /> {a.fullAddressName}
										</span>
										<span className='text-[14px] text-muted'>
											{a.street}, {a.city}, {a.country}
										</span>
										<span className='text-[14px] text-muted'>{a.phone}</span>
									</Choice>
								))}
								<Choice active={selected === NEW_ADDRESS} onClick={() => setSelected(NEW_ADDRESS)}>
									<span className='flex items-center gap-2 font-bold'>
										<Plus size={16} className='text-brand' /> Add a new address
									</span>
									<span className='text-[14px] text-muted'>Ship this order somewhere else</span>
								</Choice>
							</div>
						)}

						{addresses !== null && selected === NEW_ADDRESS && (
							<div className='flex flex-col gap-5'>
								<div className='grid grid-cols-1 gap-5 md:grid-cols-2'>
									<TextField label='Full name' name='name' autoComplete='name' value={form.name} onChange={onChange} error={errors.name} />
									<TextField
										label='Phone number'
										name='phone'
										type='tel'
										autoComplete='tel'
										placeholder='+20 100 000 0000'
										value={form.phone}
										onChange={onChange}
										error={errors.phone}
									/>
								</div>
								<TextField
									label='Street address'
									name='street'
									autoComplete='street-address'
									placeholder='12 Tahrir Street'
									value={form.street}
									onChange={onChange}
									error={errors.street}
								/>
								<div className='grid grid-cols-1 gap-5 md:grid-cols-3'>
									<TextField
										label='City'
										name='city'
										autoComplete='address-level2'
										placeholder='Cairo'
										value={form.city}
										onChange={onChange}
										error={errors.city}
									/>
									<TextField
										label='Postal code (optional)'
										name='postalCode'
										autoComplete='postal-code'
										placeholder='11511'
										maxLength={20}
										value={form.postalCode}
										onChange={onChange}
									/>
									<TextField
										label='Country'
										name='country'
										autoComplete='country-name'
										value={form.country}
										onChange={onChange}
										error={errors.country}
									/>
								</div>
								<p className='text-[13px] text-muted'>This address is saved to your account for next time.</p>
							</div>
						)}
					</Section>

					<Section number='02' title='Payment method'>
						<div className='grid grid-cols-2 gap-3'>
							{METHODS.map(({ value, label, note, icon: Icon }) => (
								<button
									key={value}
									type='button'
									onClick={() => setMethod(value)}
									className={`flex flex-col items-center gap-1 rounded p-4 transition-colors ${
										method === value ? 'bg-brand-soft text-brand-dark ring-1 ring-brand' : 'bg-panel hover:bg-panel-strong'
									}`}
								>
									<Icon size={22} />
									<span className='label'>{label}</span>
									<span className='text-[12px] text-muted'>{note}</span>
								</button>
							))}
						</div>
						<p className='flex gap-3 rounded bg-panel p-4 text-[14px] text-muted'>
							<ShieldCheck size={20} className='shrink-0 text-brand' />
							{method === 'Card'
								? 'You type your card on Fawaterak’s secure page, never on ours. Staging test card 5123 4500 0000 0008, expiry 12/26, CVV 100.'
								: 'Pay in cash when the courier arrives. Make sure your address and phone are right.'}
						</p>
					</Section>
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
					<div className='flex flex-col gap-2 rounded bg-panel p-4 text-[15px]'>
						<Row label='Subtotal' value={formatPrice(cart.subTotal)} />
						<Row label='Shipping' value={cart.shippingFee > 0 ? formatPrice(cart.shippingFee) : 'Free'} />
						<div className='mt-2 flex items-center justify-between border-t border-line pt-3'>
							<span className='label'>Grand total</span>
							<span className='text-[24px] font-bold text-brand'>{formatPrice(cart.total)}</span>
						</div>
					</div>
					<p className='text-[13px] text-muted'>The confirmation goes to {user?.email}</p>
					{serverError && (
						<p role='alert' className='text-[14px] font-bold text-red-600'>
							{serverError}
						</p>
					)}
					<button type='submit' disabled={busy || addresses === null} className='btn-primary w-full'>
						{busy ? 'Placing your order...' : method === 'Card' ? `Continue and pay ${formatPrice(cart.total)}` : 'Place order'}
						{!busy && <ArrowRight size={16} />}
					</button>
				</aside>
			</div>
		</form>
	);
};

const Section = ({ number, title, children }) => (
	<section className='card flex flex-col gap-5 p-6 md:p-8'>
		<h2 className='flex items-center gap-3 text-[18px]'>
			<span className='rounded bg-panel px-2 py-1 text-[12px] text-muted'>{number}</span>
			{title}
		</h2>
		{children}
	</section>
);

// A selectable card, orange with a check mark when chosen
const Choice = ({ active, onClick, children }) => (
	<button
		type='button'
		onClick={onClick}
		className={`relative flex flex-col items-start gap-1 rounded p-4 text-left transition-colors ${
			active ? 'bg-brand-soft ring-1 ring-brand' : 'bg-panel hover:bg-panel-strong'
		}`}
	>
		{active && (
			<span className='absolute right-3 top-3 flex h-5 w-5 items-center justify-center rounded-full bg-brand text-white'>
				<Check size={12} />
			</span>
		)}
		{children}
	</button>
);

const Row = ({ label, value }) => (
	<div className='flex items-center justify-between'>
		<span className='text-muted'>{label}</span>
		<span className='font-bold'>{value}</span>
	</div>
);

export default Checkout;
