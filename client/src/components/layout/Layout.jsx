import React from 'react';
import { Outlet } from 'react-router-dom';

import Navbar from './Navbar';
import Footer from './Footer';

// The header and footer around every page, a lazily loaded page shows a loading line until its code arrives
const Layout = () => (
	<>
		<Navbar />
		<main className='min-h-screen pt-20'>
			<React.Suspense fallback={<p className='page py-16 text-muted'>Loading...</p>}>
				<Outlet />
			</React.Suspense>
		</main>
		<Footer />
	</>
);

export default Layout;
