// The street, city and country line and the phone of a saved address, under its name
const AddressLines = ({ address }) => (
	<>
		<span className='block text-[14px] text-muted'>
			{address.street}, {address.city}, {address.country}
		</span>
		<span className='block text-[14px] text-muted'>{address.phone}</span>
	</>
);

export default AddressLines;
