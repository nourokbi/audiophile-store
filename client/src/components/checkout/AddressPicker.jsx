import { Check, MapPin, Plus } from 'lucide-react';

import AddressLines from '../shop/AddressLines';

// The saved addresses as cards plus one card to add a new address, the chosen one is orange with a check mark
const AddressPicker = ({ addresses, selectedAddressId, newAddressId, onSelect }) => (
	<div className='grid grid-cols-1 gap-3 md:grid-cols-2'>
		{addresses.map((address) => (
			<ChoiceCard key={address.id} isChosen={selectedAddressId === address.id} onClick={() => onSelect(address.id)}>
				<span className='flex items-center gap-2 font-bold'>
					<MapPin size={16} className='text-brand' /> {address.fullAddressName}
				</span>
				<AddressLines address={address} />
			</ChoiceCard>
		))}
		<ChoiceCard isChosen={selectedAddressId === newAddressId} onClick={() => onSelect(newAddressId)}>
			<span className='flex items-center gap-2 font-bold'>
				<Plus size={16} className='text-brand' /> Add a new address
			</span>
			<span className='text-[14px] text-muted'>Ship this order somewhere else</span>
		</ChoiceCard>
	</div>
);

const ChoiceCard = ({ isChosen, onClick, children }) => (
	<button
		type='button'
		onClick={onClick}
		className={`relative flex flex-col items-start gap-1 rounded p-4 text-left transition-colors ${
			isChosen ? 'bg-brand-soft ring-1 ring-brand' : 'bg-panel hover:bg-panel-strong'
		}`}
	>
		{isChosen && (
			<span className='absolute right-3 top-3 flex h-5 w-5 items-center justify-center rounded-full bg-brand text-white'>
				<Check size={12} />
			</span>
		)}
		{children}
	</button>
);

export default AddressPicker;
