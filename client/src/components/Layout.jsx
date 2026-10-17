import React from 'react';
import { Outlet } from 'react-router-dom';

import Navbar from './Navbar';
import Footer from './Footer';

const Layout = (props) => {
	return (
		<>
			<Navbar
				cart={props.cart}
				clearCart={props.clearCart}
				totalPrice={props.totalPrice}
				setTotalPrice={props.setTotalPrice}
				updateQuantity={props.updateQuantity}
				removeFromCart={props.removeFromCart}
			/>
			<Outlet />
			<Footer />
		</>
	);
};

export default Layout;
