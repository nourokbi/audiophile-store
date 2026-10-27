// The sliding pill on its own, for a button that already has its own layout
export const SwitchTrack = ({ isOn }) => (
	<span className={`relative h-6 w-11 shrink-0 rounded-full transition-colors ${isOn ? 'bg-brand' : 'bg-panel-strong'}`}>
		<span
			className={`absolute left-[2px] top-[2px] h-5 w-5 rounded-full bg-white transition-transform ${
				isOn ? 'translate-x-5' : ''
			}`}
		/>
	</span>
);

// An on and off switch, label is read by screen readers and children show as text beside it
const Switch = ({ isOn, label, disabled = false, onClick, children }) => (
	<button
		type='button'
		role='switch'
		aria-checked={isOn}
		aria-label={label}
		disabled={disabled}
		onClick={onClick}
		className='flex select-none items-center gap-3 disabled:opacity-50'
	>
		<SwitchTrack isOn={isOn} />
		{children}
	</button>
);

export default Switch;
