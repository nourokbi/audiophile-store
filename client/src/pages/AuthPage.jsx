import React from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';

import TextField from '../components/TextField';
import { useAuth } from '../context/AuthContext';

// Login and register share one page: the same card, with a name field when registering.
// After success the customer goes back to where they were sent from (?redirect=…).
const AuthPage = ({ mode }) => {
	const isRegister = mode === 'register';
	const { login, register } = useAuth();
	const navigate = useNavigate();
	const [params] = useSearchParams();
	const redirect = safeRedirect(params.get('redirect'));

	const [form, setForm] = React.useState({ name: '', email: '', password: '' });
	const [errors, setErrors] = React.useState({});
	const [serverError, setServerError] = React.useState('');
	const [busy, setBusy] = React.useState(false);

	const onChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

	const validate = () => {
		const next = {};
		if (isRegister && form.name.trim().length < 2) next.name = 'Enter your name';
		if (!/^\S+@\S+\.\S+$/.test(form.email)) next.email = 'Invalid email format';
		if (form.password.length < 8) next.password = 'At least 8 characters';
		setErrors(next);
		return Object.keys(next).length === 0;
	};

	const onSubmit = async (e) => {
		e.preventDefault();
		setServerError('');
		if (!validate()) return;

		setBusy(true);
		try {
			if (isRegister) await register(form.name.trim(), form.email.trim(), form.password);
			else await login(form.email.trim(), form.password);
			navigate(redirect, { replace: true });
		} catch (err) {
			setServerError(
				err.status === 409
					? 'An account with this email already exists. Log in instead.'
					: err.status === 401
						? 'Wrong email or password.'
						: err.status === 429
							? 'Too many attempts. Wait a minute and try again.'
							: err.message,
			);
		} finally {
			setBusy(false);
		}
	};

	const otherMode = isRegister ? '/login' : '/register';
	const keepRedirect = params.get('redirect') ? `?redirect=${encodeURIComponent(redirect)}` : '';

	return (
		<main className='mt-[95px] bg-gray-light'>
			<div className='xs:px-[24px] md:px-[40px] lg:px-[165px] xs:pt-10 md:pt-16 lg:pt-[79px] pb-[140px] flex justify-center'>
				<section className='bg-white xs:p-6 md:p-12 rounded-[8px] w-full max-w-[540px]'>
					<p className='text-cream text-[13px] font-bold uppercase leading-[25px] tracking-wide'>
						{isRegister ? 'New customer' : 'Welcome back'}
					</p>
					<h3 className='mt-2 xs:text-[28px] md:text-[32px]'>{isRegister ? 'Create account' : 'Log in'}</h3>
					<p className='p text-black/50 mt-4'>
						{isRegister
							? 'One account for your cart, your orders and their delivery status.'
							: 'Log in to see your cart and finish your order.'}
					</p>

					<form onSubmit={onSubmit} className='mt-8 flex flex-col gap-6' noValidate>
						{isRegister && (
							<TextField
								label='Name'
								name='name'
								autoComplete='name'
								placeholder='Alexei Ward'
								value={form.name}
								onChange={onChange}
								error={errors.name}
							/>
						)}
						<TextField
							label='Email address'
							name='email'
							type='email'
							autoComplete='email'
							placeholder='alexei@mail.com'
							value={form.email}
							onChange={onChange}
							error={errors.email}
						/>
						<TextField
							label='Password'
							name='password'
							type='password'
							autoComplete={isRegister ? 'new-password' : 'current-password'}
							placeholder='At least 8 characters'
							value={form.password}
							onChange={onChange}
							error={errors.password}
						/>

						{serverError && (
							<p role='alert' className='text-red-600 text-[14px] font-bold'>
								{serverError}
							</p>
						)}

						<button
							type='submit'
							disabled={busy}
							className='btn w-full bg-cream hover:bg-cream-light transition-all duration-150 disabled:opacity-60 disabled:cursor-wait'
						>
							{busy ? 'Please wait…' : isRegister ? 'Create account' : 'Log in'}
						</button>
					</form>

					<p className='p text-black/50 mt-6 text-center'>
						{isRegister ? 'Already have an account? ' : 'New to Audiophile? '}
						<Link to={otherMode + keepRedirect} className='text-cream font-bold hover:text-cream-light'>
							{isRegister ? 'Log in' : 'Create an account'}
						</Link>
					</p>
				</section>
			</div>
		</main>
	);
};

// Only paths inside the shop, so a crafted ?redirect=https://evil.example can't send people away.
function safeRedirect(value) {
	return value && value.startsWith('/') && !value.startsWith('//') ? value : '/';
}

export default AuthPage;
