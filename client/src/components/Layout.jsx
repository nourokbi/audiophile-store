import { Outlet } from 'react-router-dom';

import Navbar from './Navbar';
import Footer from './Footer';

const Layout = () => (
	<>
		<Navbar />
		<main className='min-h-screen pt-20'>
			<Outlet />
		</main>
		<Footer />
	</>
);

export default Layout;
