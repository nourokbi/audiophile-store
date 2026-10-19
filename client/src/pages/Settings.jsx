import React from 'react';
import { MapPin, Moon, Sun, Trash2, User } from 'lucide-react';

import { useAuth } from '../context/AuthContext';
import { useTheme } from '../context/ThemeContext';
import { api } from '../lib/api';

// The account page, the profile is read only until the account API can edit it
const Settings = () => {
	const { user } = useAuth();
	const { theme, setTheme } = useTheme();
	const [addresses, setAddresses] = React.useState(null);
	const [error, setError] = React.useState('');

	React.useEffect(() => {
		api('/addresses')
			.then(setAddresses)
			.catch((e) => setError(e.message));
	}, []);

	const removeAddress = async (id) => {
		if (!window.confirm('Remove this address? Past orders keep their own copy.')) return;
		try {
			await api(`/addresses/${id}`, { method: 'DELETE' });
			setAddresses((list) => list.filter((a) => a.id !== id));
		} catch (e) {
			setError(e.message);
		}
	};

	return (
		<div className='page flex flex-col gap-8 pb-24 pt-10'>
			<div>
				<p className='eyebrow'>My account</p>
				<h1 className='title mt-2'>Settings</h1>
			</div>

			<div className='grid grid-cols-1 items-start gap-6 lg:grid-cols-2'>
				<Section icon={User} title='Profile'>
					<Field label='Name' value={user.name} />
					<Field label='Email address' value={user.email} />
					<Field label='Role' value={user.roles?.join(', ')} />
					<p className='text-[13px] text-muted'>Changing your name, email or password comes with the account API.</p>
				</Section>

				<Section icon={theme === 'dark' ? Moon : Sun} title='Appearance'>
					<div className='grid grid-cols-2 gap-3'>
						{[
							{ value: 'light', label: 'Light', icon: Sun },
							{ value: 'dark', label: 'Dark', icon: Moon },
						].map(({ value, label, icon: Icon }) => (
							<button
								key={value}
								onClick={() => setTheme(value)}
								className={`flex flex-col items-center gap-2 rounded p-5 transition-colors ${
									theme === value ? 'bg-brand-soft text-brand-dark ring-1 ring-brand' : 'bg-panel hover:bg-panel-strong'
								}`}
							>
								<Icon size={22} />
								<span className='label'>{label}</span>
							</button>
						))}
					</div>
				</Section>

				<Section icon={MapPin} title='Saved addresses' className='lg:col-span-2'>
					{error && <p className='text-[14px] font-bold text-red-600'>{error}</p>}
					{!addresses && !error && <p className='text-muted'>Loading...</p>}
					{addresses?.length === 0 && <p className='text-muted'>No saved address yet, checkout saves the one you use.</p>}
					<ul className='grid grid-cols-1 gap-3 md:grid-cols-2'>
						{addresses?.map((a) => (
							<li key={a.id} className='flex items-start justify-between gap-4 rounded bg-panel p-4'>
								<div className='text-[14px]'>
									<p className='font-bold'>{a.fullAddressName}</p>
									<p className='text-muted'>
										{a.street}, {a.city}, {a.country}
									</p>
									<p className='text-muted'>{a.phone}</p>
								</div>
								<button aria-label='Remove address' onClick={() => removeAddress(a.id)} className='text-muted hover:text-red-600'>
									<Trash2 size={16} />
								</button>
							</li>
						))}
					</ul>
				</Section>
			</div>
		</div>
	);
};

const Section = ({ icon: Icon, title, className = '', children }) => (
	<section className={`card flex flex-col gap-5 p-6 ${className}`}>
		<h2 className='flex items-center gap-3 text-[18px]'>
			<span className='flex h-9 w-9 items-center justify-center rounded bg-panel text-brand'>
				<Icon size={18} />
			</span>
			{title}
		</h2>
		{children}
	</section>
);

const Field = ({ label, value }) => (
	<div className='flex flex-col gap-1'>
		<span className='text-[12px] font-bold uppercase tracking-wider text-muted'>{label}</span>
		<span className='text-[15px] font-medium'>{value}</span>
	</div>
);

export default Settings;
