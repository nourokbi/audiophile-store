import React from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowRight, AtSign, Eye, EyeOff, Lock, ShieldCheck, User } from 'lucide-react';

import TextField from '../components/ui/TextField';
import { useAuth } from '../context/AuthContext';

// Login and register share one card, register adds the name field, and success goes back to the redirect page
const AuthPage = ({ mode }) => {
	const isRegister = mode === 'register';
	const { login, register } = useAuth();
	const navigate = useNavigate();
	const [searchParams] = useSearchParams();
	const redirect = safeRedirect(searchParams.get('redirect'));

	const [form, setForm] = React.useState({ name: '', email: '', password: '' });
	const [showPassword, setShowPassword] = React.useState(false);
	const [fieldErrors, setFieldErrors] = React.useState({});
	const [serverError, setServerError] = React.useState('');
	const [isSubmitting, setIsSubmitting] = React.useState(false);

	const updateField = (event) => setForm({ ...form, [event.target.name]: event.target.value });

	const validateForm = () => {
		const foundErrors = {};
		if (isRegister && form.name.trim().length < 3) foundErrors.name = 'At least 3 characters';
		if (!/^\S+@\S+\.\S+$/.test(form.email)) foundErrors.email = 'Invalid email';
		if (form.password.length < 8) foundErrors.password = 'At least 8 characters';
		setFieldErrors(foundErrors);
		return Object.keys(foundErrors).length === 0;
	};

	const submitForm = async (event) => {
		event.preventDefault();
		setServerError('');
		if (!validateForm()) return;

		setIsSubmitting(true);
		try {
			if (isRegister) await register(form.name.trim(), form.email.trim(), form.password);
			else await login(form.email.trim(), form.password);
			navigate(redirect, { replace: true });
		} catch (error) {
			setServerError(ERROR_MESSAGES[error.status] ?? error.message);
		} finally {
			setIsSubmitting(false);
		}
	};

	const redirectQuery = searchParams.get('redirect') ? `?redirect=${encodeURIComponent(redirect)}` : '';

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

					<form onSubmit={submitForm} className='flex flex-col gap-5' noValidate>
						{isRegister && (
							<TextField
								label='Name'
								name='name'
								icon={User}
								autoComplete='name'
								placeholder='Alexei Ward'
								value={form.name}
								onChange={updateField}
								error={fieldErrors.name}
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
							onChange={updateField}
							error={fieldErrors.email}
						/>
						<TextField
							label='Password'
							name='password'
							type={showPassword ? 'text' : 'password'}
							icon={Lock}
							autoComplete={isRegister ? 'new-password' : 'current-password'}
							placeholder='At least 8 characters'
							value={form.password}
							onChange={updateField}
							error={fieldErrors.password}
							trailing={
								<button
									type='button'
									aria-label={showPassword ? 'Hide password' : 'Show password'}
									onClick={() => setShowPassword((isShown) => !isShown)}
									className='text-muted hover:text-ink'
								>
									{showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
								</button>
							}
						/>

						{serverError && (
							<p role='alert' className='error-text'>
								{serverError}
							</p>
						)}

						<button type='submit' disabled={isSubmitting} className='btn-primary w-full'>
							{isSubmitting ? 'Please wait...' : isRegister ? 'Create account' : 'Log in'}
							{!isSubmitting && <ArrowRight size={16} />}
						</button>
					</form>
				</div>

				<div className='flex flex-col items-center gap-2 bg-panel px-8 py-6 text-center'>
					<p className='text-[15px]'>
						{isRegister ? 'Already have an account? ' : 'New to Audiophile? '}
						<Link
							to={(isRegister ? '/login' : '/register') + redirectQuery}
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

const ERROR_MESSAGES = {
	409: 'An account with this email already exists. Log in instead.',
	401: 'Wrong email or password.',
	429: 'Too many attempts. Wait a minute and try again.',
};

// Only paths inside the shop, so a crafted redirect cannot send people to another site
function safeRedirect(value) {
	return value && value.startsWith('/') && !value.startsWith('//') ? value : '/';
}

export default AuthPage;
