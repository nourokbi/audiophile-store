import React from 'react';
import { Link } from 'react-router-dom';

import CoDLogo from '/assets/checkout/icon-cash-on-delivery.svg';
import successLogo from '/assets/checkout/icon-order-confirmation.svg';

const Checkout = (props) => {
	const [success, setSuccess] = React.useState(false);

	const [selectedPaymentMethod, setSelectedPaymentMethod] =
		React.useState('e-Money');

	const handlePaymentMethodChange = (event) => {
		setSelectedPaymentMethod(event.target.value);
	};

	// State variables for form input values and error messages
	const [formData, setFormData] = React.useState({
		name: '',
		email: '',
		phoneNumber: '',
		address: '',
		zipCode: '',
		city: '',
		country: '',
		eMoneyNumber: '',
		eMoneyPin: '',
	});

	const [formErrors, setFormErrors] = React.useState({
		email: '',
		phoneNumber: '',
	});

	// Validation functions
	const validateEmail = (email) => {
		// Add your email validation logic here
		// For example, you can use a regular expression
		return /^\S+@\S+\.\S+$/.test(email);
	};

	const validatePhoneNumber = (phoneNumber) => {
		// Add your phone number validation logic here
		// For example, you can check if it matches a valid phone number format
		return /^\+\d{1,3} \d{3}-\d{3}-\d{4}$|^\d{9}$/.test(phoneNumber);
	};

	// Handle input changes
	const handleInputChange = (e) => {
		const { name, value } = e.target;
		setFormData({ ...formData, [name]: value });
	};

	const handleFormSubmit = (e) => {
		e.preventDefault();

		// Initialize an object to hold error messages
		const newFormErrors = {};

		// Check for empty fields and update error messages
		if (formData.name.trim() === '') {
			newFormErrors.name = 'Name is required';
		}
		if (formData.address.trim() === '') {
			newFormErrors.address = 'Address is required';
		}
		if (formData.zipCode.trim() === '') {
			newFormErrors.zipCode = 'ZIP Code is required';
		}
		if (formData.city.trim() === '') {
			newFormErrors.city = 'City is required';
		}
		if (formData.country.trim() === '') {
			newFormErrors.country = 'Country is required';
		}

		if (formData.email.trim() === '') {
			newFormErrors.email = 'Email is required';
		} else {
			// Perform email validation if it's not empty
			const emailIsValid = validateEmail(formData.email);
			if (!emailIsValid) {
				newFormErrors.email = 'Invalid email format';
			}
		}

		if (formData.phoneNumber.trim() === '') {
			newFormErrors.phoneNumber = 'Phone number is required';
		} else {
			// Perform phone number validation if it's not empty
			const phoneNumberIsValid = validatePhoneNumber(formData.phoneNumber);
			if (!phoneNumberIsValid) {
				newFormErrors.phoneNumber = 'Invalid phone number format';
			}
		}

		// Check for empty e-Money inputs if e-Money is selected
		if (selectedPaymentMethod === 'e-Money') {
			if (formData.eMoneyNumber.trim() === '') {
				newFormErrors.eMoneyNumber = 'Number is required';
			}
			if (formData.eMoneyPin.trim() === '') {
				newFormErrors.eMoneyPin = 'PIN is required';
			}
		}

		// Update the formErrors state with the new error messages
		setFormErrors(newFormErrors);

		// Check if any error messages exist (i.e., form is invalid)
		const isFormInvalid = Object.values(newFormErrors).some(
			(error) => error !== '',
		);

		if (!isFormInvalid) {
			setSuccess(true);
		}
	};

	const cartItems = props.cart.map((item, index) => {
		return (
			<div key={index} className='w-full flex items-center justify-between'>
				<div className='flex items-center gap-4'>
					<div>
						<img src={item.image} className='w-16 h-16 rounded-[8px]' />
					</div>
					<div className='flex flex-col'>
						<p className='text-[15px] font-bold leading-[25px] text-black'>
							{item.name}
						</p>
						<p className='text-[14px] font-bold leading-[25px] text-black/50'>
							$ {item.price}
						</p>
					</div>
				</div>
				<div className='flex p-[15px] items-center'>
					<p className='text-[15px] font-bold text-black/50'>
						x{item.quantity}
					</p>
				</div>
			</div>
		);
	});

	return (
		<main className='mt-[95px] bg-gray-light'>
			<div className='xs:px-[24px] md:px-[40px] lg:px-[165px] xs:pt-4 md:pt-8 lg:pt-[79px]'>
				<Link relative='path' to='..'>
					<button className='p text-black/50 cursor-pointer hover:text-cream transition-all duration-150'>
						Go back
					</button>
				</Link>
			</div>
			<div className='xs:px-[24px] md:px-[40px] lg:px-[165px] mt-[38px] pb-[140px] flex xs:flex-col xl:flex-row gap-8 xs:items-center xl:items-start'>
				<section className='bg-white p-12 rounded-[8px] w-full'>
					<h3>Checkout</h3>
					<form onSubmit={handleFormSubmit}>
						<div className='mt-10'>
							<p className='text-cream text-[13px] font-bold uppercase leading-[25px] tracking-wide'>
								Billing Details
							</p>
							<div className='mt-4 flex flex-col gap-6'>
								<div className='flex xs:flex-col md:flex-row items-center gap-4'>
									<div className='flex flex-col w-full'>
										<div className='flex items-center justify-between'>
											<label
												className={`text-xs font-bold ${
													formErrors.name ? 'text-red-600' : 'text-black'
												}`}
											>
												Name
											</label>
											<p className='text-red-600 text-xs font-medium'>
												{formErrors.name}
											</p>
										</div>
										<input
											className={`placeholder:text-[14px] placeholder:text-black/40 placeholder:font-bold placeholder:leading-[-0.25px] text-black font-bold py-[18px] px-[24px] bg-white rounded-lg border border-stone-300 focus:outline-cream caret-cream mt-[9px] ${
												formErrors.name ? 'border-red-600' : ''
											}`}
											type='text'
											name='name'
											placeholder='Alexei Ward'
											value={formData.name}
											onChange={handleInputChange}
										/>
									</div>
									<div className='flex flex-col w-full'>
										<div className='flex items-center justify-between'>
											<label
												className={`text-xs font-bold ${
													formErrors.email ? 'text-red-600' : 'text-black'
												}`}
											>
												Email address
											</label>
											<p className='text-red-600 text-xs font-medium'>
												{formErrors.email}
											</p>
										</div>
										<input
											className={`placeholder:text-[14px] placeholder:text-black/40 placeholder:font-bold placeholder:leading-[-0.25px] text-black font-bold py-[18px] px-[24px] bg-white rounded-lg border border-stone-300 focus:outline-cream caret-cream mt-[9px] ${
												formErrors.email ? 'border-red-600' : ''
											}`}
											type='text'
											name='email'
											placeholder='alexei@mail.com'
											value={formData.email}
											onChange={handleInputChange}
										/>
									</div>
								</div>
								<div className='flex flex-col'>
									<div className='flex items-center justify-between'>
										<label
											className={`text-xs font-bold ${
												formErrors.phoneNumber ? 'text-red-600' : 'text-black'
											}`}
										>
											Phone number
										</label>
										<p className='text-red-600 text-xs font-medium'>
											{formErrors.phoneNumber}
										</p>
									</div>
									<input
										className={`placeholder:text-[14px] placeholder:text-black/40 placeholder:font-bold placeholder:leading-[-0.25px] text-black font-bold py-[18px] px-[24px] bg-white rounded-lg border border-stone-300 focus:outline-cream caret-cream mt-[9px] ${
											formErrors.phoneNumber ? 'border-red-600' : ''
										}`}
										type='text'
										name='phoneNumber'
										placeholder='+1 (202) 555-0136'
										value={formData.phoneNumber}
										onChange={handleInputChange}
									/>
								</div>
							</div>
						</div>
						<div className='mt-[53px]'>
							<p className='text-cream text-[13px] font-bold uppercase leading-[25px] tracking-wide'>
								Shipping Info
							</p>
							<div className='mt-4 flex flex-col gap-6'>
								<div className='flex flex-col'>
									<div className='flex items-center justify-between'>
										<label
											className={`text-xs font-bold ${
												formErrors.address ? 'text-red-600' : 'text-black'
											}`}
										>
											Address
										</label>
										<p className='text-red-600 text-xs font-medium'>
											{formErrors.address}
										</p>
									</div>
									<input
										className={`placeholder:text-[14px] placeholder:text-black/40 placeholder:font-bold placeholder:leading-[-0.25px] text-black font-bold py-[18px] px-[24px] bg-white rounded-lg border border-stone-300 focus:outline-cream caret-cream mt-[9px] ${
											formErrors.address ? 'border-red-600' : ''
										}`}
										type='text'
										name='address'
										placeholder='1137 Williams Avenue'
										value={formData.address}
										onChange={handleInputChange}
									/>
								</div>
								<div className='flex xs:flex-col md:flex-row items-center gap-4'>
									<div className='flex flex-col w-full'>
										<div className='flex items-center justify-between'>
											<label
												className={`text-xs font-bold ${
													formErrors.zipCode ? 'text-red-600' : 'text-black'
												}`}
											>
												ZIP Code
											</label>
											<p className='text-red-600 text-xs font-medium'>
												{formErrors.zipCode}
											</p>
										</div>
										<input
											className={`placeholder:text-[14px] placeholder:text-black/40 placeholder:font-bold placeholder:leading-[-0.25px] text-black font-bold py-[18px] px-[24px] bg-white rounded-lg border border-stone-300 focus:outline-cream caret-cream mt-[9px] ${
												formErrors.zipCode ? 'border-red-600' : ''
											}`}
											type='text'
											name='zipCode'
											placeholder='10001'
											maxLength={5}
											value={formData.zipCode}
											onChange={handleInputChange}
										/>
									</div>
									<div className='flex flex-col w-full'>
										<div className='flex items-center justify-between'>
											<label
												className={`text-xs font-bold ${
													formErrors.city ? 'text-red-600' : 'text-black'
												}`}
											>
												City
											</label>
											<p className='text-red-600 text-xs font-medium'>
												{formErrors.city}
											</p>
										</div>
										<input
											className={`placeholder:text-[14px] placeholder:text-black/40 placeholder:font-bold placeholder:leading-[-0.25px] text-black font-bold py-[18px] px-[24px] bg-white rounded-lg border border-stone-300 focus:outline-cream caret-cream mt-[9px] ${
												formErrors.city ? 'border-red-600' : ''
											}`}
											type='text'
											name='city'
											placeholder='New York'
											value={formData.city}
											onChange={handleInputChange}
										/>
									</div>
								</div>
								<div className='flex flex-col'>
									<div className='flex items-center justify-between'>
										<label
											className={`text-xs font-bold ${
												formErrors.country ? 'text-red-600' : 'text-black'
											}`}
										>
											Country
										</label>
										<p className='text-red-600 text-xs font-medium'>
											{formErrors.country}
										</p>
									</div>
									<input
										className={`placeholder:text-[14px] placeholder:text-black/40 placeholder:font-bold placeholder:leading-[-0.25px] text-black font-bold py-[18px] px-[24px] bg-white rounded-lg border border-stone-300 focus:outline-cream caret-cream mt-[9px] ${
											formErrors.country ? 'border-red-600' : ''
										}`}
										type='text'
										name='country'
										placeholder='United States'
										value={formData.country}
										onChange={handleInputChange}
									/>
								</div>
							</div>
						</div>
						<div className='mt-[61px]'>
							<div className='flex items-center justify-between'>
								<p className='text-cream text-[13px] font-bold uppercase leading-[25px] tracking-wide'>
									Payment Details
								</p>
							</div>

							<div className='flex xs:flex-col xs:gap-[17px] md:gap-0 md:flex-row mt-4 justify-between'>
								<label className='text-black text-xs font-bold flex-1'>
									Payment Methods
								</label>
								<div className='flex flex-col flex-1 gap-4'>
									<div className='flex items-center gap-4 p-[18px] rounded-lg border border-stone-300 hover:border-cream transition-all duration-150'>
										<input
											type='radio'
											className='cursor-pointer checked:accent-cream checked:border-cream'
											name='payment'
											value='e-Money'
											checked={selectedPaymentMethod === 'e-Money'}
											onChange={handlePaymentMethodChange}
										/>
										<label className='text-black text-xs font-bold'>
											e-Money
										</label>
									</div>
									<div className='flex items-center gap-4 p-[18px] rounded-lg border border-stone-300 hover:border-cream transition-all duration-150'>
										<input
											type='radio'
											className='cursor-pointer checked:accent-cream checked:border-cream'
											name='payment'
											value='Cash on Delivery'
											checked={selectedPaymentMethod === 'Cash on Delivery'}
											onChange={handlePaymentMethodChange}
										/>
										<label className='text-black text-xs font-bold'>
											Cash on Delivery
										</label>
									</div>
								</div>
							</div>
						</div>

						{selectedPaymentMethod === 'e-Money' && (
							<div className='flex xs:flex-col md:flex-row items-center gap-4 mt-4'>
								{/* Render additional inputs for e-Money */}
								<div className='flex flex-col w-full'>
									<div className='flex items-center justify-between'>
										<label
											className={`text-xs font-bold ${
												formErrors.eMoneyNumber ? 'text-red-600' : 'text-black'
											}`}
										>
											e-Money Number
										</label>
										<p className='text-red-600 text-xs font-medium'>
											{formErrors.eMoneyNumber}
										</p>
									</div>
									<input
										type='text'
										placeholder='238521993'
										className={`placeholder:text-[14px] placeholder:text-black/40 placeholder:font-bold placeholder:leading-[-0.25px] text-black font-bold py-[18px] px-[24px] bg-white rounded-lg border border-stone-300 focus:outline-cream caret-cream mt-[9px] ${
											formErrors.eMoneyNumber ? 'border-red-600' : ''
										}`}
										maxLength={9}
									/>
								</div>
								<div className='flex flex-col w-full'>
									<div className='flex items-center justify-between'>
										<label
											className={`text-xs font-bold ${
												formErrors.eMoneyPin ? 'text-red-600' : 'text-black'
											}`}
										>
											e-Money PIN
										</label>
										<p className='text-red-600 text-xs font-medium'>
											{formErrors.eMoneyPin}
										</p>
									</div>
									<input
										type='text'
										placeholder='6891'
										className={`placeholder:text-[14px] placeholder:text-black/40 placeholder:font-bold placeholder:leading-[-0.25px] text-black font-bold py-[18px] px-[24px] bg-white rounded-lg border border-stone-300 focus:outline-cream caret-cream mt-[9px] ${
											formErrors.eMoneyPin ? 'border-red-600' : ''
										}`}
										maxLength={4}
									/>
								</div>
							</div>
						)}

						{selectedPaymentMethod === 'Cash on Delivery' && (
							<div className='mt-[30px] flex xs:flex-col md:flex-row items-center gap-8'>
								<img src={CoDLogo} />
								<p className='text-black/50 text-[15px] font-medium leading-[25px]'>
									The ‘Cash on Delivery’ option enables you to pay in cash when
									our delivery courier arrives at your residence. Just make sure
									your address is correct so that your order will not be
									cancelled.
								</p>
							</div>
						)}
					</form>
				</section>
				<section className='rounded-[8px] p-8 bg-white xs:w-full xl:w-auto'>
					<div>
						<h6 className='h6 text-black'>Summary</h6>
					</div>
					<div className='flex flex-col items-center gap-6 py-8 overflow-y-auto max-h-[304px] xs:w-full lg:w-[284px]'>
						{cartItems}
					</div>

					<div className='flex flex-col gap-2'>
						<div className='flex items-center justify-between'>
							<p className='text-[15px] font-[500] leading-[25px] text-black/50 uppercase'>
								Total
							</p>
							<h6 className='font-bold leading-[25px] text-black'>
								$ {props.totalPrice}
							</h6>
						</div>
						{/* If shipping price and VAT would change, I'd prepare a seperate file with them, but for more readable code I will leave them as they are */}
						<div className='flex items-center justify-between'>
							<p className='text-[15px] font-[500] leading-[25px] text-black/50 uppercase'>
								Shipping
							</p>
							<h6 className='font-bold leading-[25px] text-black'>$ 50</h6>
						</div>
						<div className='flex items-center justify-between'>
							<p className='text-[15px] font-[500] leading-[25px] text-black/50 uppercase'>
								Vat (Included)
							</p>
							<h6 className='font-bold leading-[25px] text-black'>
								$ {Math.floor(props.totalPrice * 0.2)}
							</h6>
						</div>
						<div className='flex items-center justify-between mt-4'>
							<p className='text-[15px] font-[500] leading-[25px] text-black/50 uppercase'>
								Grand Total
							</p>
							<h6 className='font-bold leading-[25px] text-cream'>
								$ {Math.floor(props.totalPrice + 50 + props.totalPrice * 0.2)}
							</h6>
						</div>
					</div>
					<div className='mt-8'>
						<button
							onClick={handleFormSubmit}
							className='btn w-full bg-cream text-white hover:bg-cream-light transition-all duration-150'
						>
							Continue & Pay
						</button>
					</div>
				</section>
			</div>
			{/* Success message */}
			{success && (
				<>
					<div className='fixed top-0 bottom-0 left-0 right-0 bg-black/50 z-[50]' />
					<section className='fixed xs:left-4 xs:right-4 md:left-[20%] lg:left-[35%] xs:top-32 md:top-[222px] bg-white xs:w-auto md:w-[540px] h-[581px] flex flex-col justify-center xs:p-8 md:p-12 z-50 rounded-lg'>
						<div>
							<img src={successLogo} />
						</div>
						<h3 className='xs:mt-[23px] md:mt-[33px]'>
							Thank you for your order
						</h3>
						<p className='p text-black/50 xs:mt-4 md:mt-6'>
							You will receive an email confirmation shortly.
						</p>
						<div className='flex xs:flex-col md:flex-row items-center xs:mt-4 md:mt-[33px] w-full'>
							<div className='bg-gray-light xs:rounded-tl-lg xs:rounded-tr-lg md:rounded-tr-none md:rounded-bl-lg flex md:flex-1 flex-col gap-2 xs:max-h-[92px] md:max-h-[280px] w-full overflow-y-auto'>
								{cartItems}
							</div>
							<div className='bg-stone xs:rounded-bl-lg md:rounded-bl-none md:rounded-tr-lg xs:rounded-br-lg flex md:flex-1 gap-2 xs:h-[92px] md:h-full flex-col justify-center w-full'>
								<p className='text-[15px] font-[500] leading-[25px] text-white/50 uppercase ml-8'>
									Grand Total
								</p>
								<h6 className='font-bold leading-[25px] ml-8 text-white'>
									$ {Math.floor(props.totalPrice + 50 + props.totalPrice * 0.2)}
								</h6>
							</div>
						</div>
						<Link to='/' className='w-full'>
							<button className='btn bg-cream hover:bg-cream-light transition-all duration-150 mt-[46px] w-full'>
								Back to Home
							</button>
						</Link>
					</section>
				</>
			)}
		</main>
	);
};

export default Checkout;
