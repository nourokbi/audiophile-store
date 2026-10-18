import { BrowserRouter, Routes, Route } from 'react-router-dom';

import Home from './pages/Home';
import Category from './pages/Category';
import Checkout from './pages/Checkout';
import Product from './pages/Product';
import AuthPage from './pages/AuthPage';
import Orders from './pages/Orders';
import OrderDetail from './pages/OrderDetail';
import Layout from './components/Layout';
import RequireAuth from './components/RequireAuth';
import { AuthProvider } from './context/AuthContext';
import { CartProvider } from './context/CartContext';
import { CatalogProvider } from './context/CatalogContext';

import data from './data/data.json';

// Category pages are presentation (images, copy) and still read data.json.
const categories = {
	headphones: { title: 'Headphones', products: data.filter((item) => item.category === 'headphones') },
	speakers: { title: 'Speakers', products: data.filter((item) => item.category === 'speakers') },
	earphones: { title: 'Earphones', products: data.filter((item) => item.category === 'earphones') },
};

// Auth first (who you are), then the cart (yours), then the catalogue (prices and stock).
const App = () => {
	return (
		<AuthProvider>
			<CartProvider>
				<CatalogProvider>
					<BrowserRouter>
						<Routes>
							<Route element={<Layout />}>
								<Route path='/' element={<Home />} />
								<Route path='/login' element={<AuthPage mode='login' />} />
								<Route path='/register' element={<AuthPage mode='register' />} />
								<Route
									path='/checkout'
									element={
										<RequireAuth>
											<Checkout />
										</RequireAuth>
									}
								/>
								<Route
									path='/orders'
									element={
										<RequireAuth>
											<Orders />
										</RequireAuth>
									}
								/>
								<Route
									path='/orders/:id'
									element={
										<RequireAuth>
											<OrderDetail />
										</RequireAuth>
									}
								/>
								<Route path=':category' element={<Category categories={categories} />} />
								<Route path=':category/:slug' element={<Product />} />
							</Route>
						</Routes>
					</BrowserRouter>
				</CatalogProvider>
			</CartProvider>
		</AuthProvider>
	);
};

export default App;
