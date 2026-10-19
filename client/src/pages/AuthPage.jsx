import React from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowRight, AtSign, Eye, EyeOff, Lock, ShieldCheck, User } from 'lucide-react';

import TextField from '../components/TextField';
import { useAuth } from '../context/AuthContext';

// Login and register share one card, register adds the name field, and success goes back to the redirect page
const AuthPage = ({ mode }) => {
	const isRegister = mode === 'register';
	const { login, register } = useAuth();
	const navigate = useNavigate();
	const [params] = useSearchParams();
	const redirect = safeRedirect(params.get('redirect'));

	const [form, setForm] = React.useState({ name: '', email: '', password: '' });
	const [showPassword, setShowPassword] = React.useState(false);
	const [errors, setErrors] = React.useState({});
	const [serverError, setServerError] = React.useState('');
	const [busy, setBusy] = React.useState(false);

	const onChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

	const validate = () => {
		const next = {};
		if (isRegister && form.name.trim().length < 3) next.name = 'At least 3 characters';
		if (!/^\S+@\S+\.\S+$/.test(form.email)) next.email = 'Invalid email';
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
			setServerError(MESSAGES[err.status] ?? err.message);
		} finally {
			setBusy(false);
		}
	};

	const keepRedirect = params.get('redirect') ? `?redirect=${encodeURIComponent(redirect)}` : '';

	return (
		<div className='flex justify-center px-6 py-16'>
			<section className='card w-full max-w-[440px] overflow-hidden shadow-lg'>
				<div className='flex flex-col gap-6 p-8'>
					<p className='label flex items-center gap-2 rounded bg-panel px-3 py-2 text-[12px]'>
						<span className='h-2 w-2 rounded-full bg-brand' />
						{isRegister ? 'New customer' : 'Welcome back'}
					</p>
					<div>
						<h1 className='text-[28px]'>{isRegister ? 'Create account' : 'Log in'}</h1>
						<p className='mt-2 text-[15px] text-muted'>
							{isRegister
								? 'One account for your cart, your orders and their delivery status.'
								: 'Log in to see your cart and track your orders.'}
						</p>
					</div>

					<form onSubmit={onSubmit} className='flex flex-col gap-5' noValidate>
						{isRegister && (
							<TextField
								label='Name'
								name='name'
								icon={User}
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
							icon={AtSign}
							autoComplete='email'
							placeholder='alexei@mail.com'
							value={form.email}
							onChange={onChange}
							error={errors.email}
						/>
						<TextField
							label='Password'
							name='password'
							type={showPassword ? 'text' : 'password'}
							icon={Lock}
							autoComplete={isRegister ? 'new-password' : 'current-password'}
							placeholder='At least 8 characters'
							value={form.password}
							onChange={onChange}
							error={errors.password}
							trailing={
								<button
									type='button'
									aria-label={showPassword ? 'Hide password' : 'Show password'}
									onClick={() => setShowPassword((v) => !v)}
									className='text-muted hover:text-ink'
								>
									{showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
								</button>
							}
						/>

						{serverError && (
							<p role='alert' className='text-[14px] font-bold text-red-600'>
								{serverError}
							</p>
						)}

						<button type='submit' disabled={busy} className='btn-primary w-full'>
							{busy ? 'Please wait...' : isRegister ? 'Create account' : 'Log in'}
							{!busy && <ArrowRight size={16} />}
						</button>
					</form>
				</div>

				<div className='flex flex-col items-center gap-2 bg-panel px-8 py-6 text-center'>
					<p className='text-[15px]'>
						{isRegister ? 'Already have an account? ' : 'New to Audiophile? '}
						<Link
							to={(isRegister ? '/login' : '/register') + keepRedirect}
							className='label text-brand-dark underline-offset-4 hover:underline'
						>
							{isRegister ? 'Log in' : 'Create an account'}
						</Link>
					</p>
					<p className='flex items-center gap-1 text-[12px] text-muted'>
						<ShieldCheck size={14} /> Passwords are hashed and never stored as text
					</p>
				</div>
			</section>
		</div>
	);
};

const MESSAGES = {
	409: 'An account with this email already exists. Log in instead.',
	401: 'Wrong email or password.',
	429: 'Too many attempts. Wait a minute and try again.',
};

// Only paths inside the shop, so a crafted redirect cannot send people to another site
function safeRedirect(value) {
	return value && value.startsWith('/') && !value.startsWith('//') ? value : '/';
}

export default AuthPage;
