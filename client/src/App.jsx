import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';

import Layout from './components/layout/Layout';
import RequireAuth from './components/layout/RequireAuth';
import { AuthProvider } from './context/AuthContext';
import { CartProvider } from './context/CartContext';
import { CatalogProvider } from './context/CatalogContext';
import { ThemeProvider } from './context/ThemeContext';
import AuthPage from './pages/AuthPage';
import Cart from './pages/Cart';
import Checkout from './pages/Checkout';
import Home from './pages/Home';
import OrderDetail from './pages/OrderDetail';
import Orders from './pages/Orders';
import Product from './pages/Product';
import Settings from './pages/Settings';

// Only admins open the dashboard, so its code loads on the first visit to /admin instead of with the shop
const AdminLayout = React.lazy(() => import('./components/admin/AdminLayout'));
const AdminOverview = React.lazy(() => import('./pages/admin/Overview'));
const AdminProducts = React.lazy(() => import('./pages/admin/Products'));
const AdminOrders = React.lazy(() => import('./pages/admin/Orders'));
const AdminOrderDetail = React.lazy(() => import('./pages/admin/OrderDetail'));
const AdminIncome = React.lazy(() => import('./pages/admin/Income'));

// The theme first, then who you are, your cart, and the catalogue
const App = () => (
	<ThemeProvider>
		<AuthProvider>
			<CartProvider>
				<CatalogProvider>
					<BrowserRouter>
						<Routes>
							<Route element={<Layout />}>
								<Route path='/' element={<Home />} />
								<Route path='/new' element={<Home collection='new' />} />
								<Route path='/cart' element={<Cart />} />
								<Route path='/login' element={<AuthPage mode='login' />} />
								<Route path='/register' element={<AuthPage mode='register' />} />
								<Route element={<RequireAuth />}>
									<Route path='/checkout' element={<Checkout />} />
									<Route path='/orders' element={<Orders />} />
									<Route path='/orders/:id' element={<OrderDetail />} />
									<Route path='/settings' element={<Settings />} />
								</Route>
								<Route element={<RequireAuth adminOnly />}>
									<Route path='/admin' element={<AdminLayout />}>
										<Route index element={<AdminOverview />} />
										<Route path='products' element={<AdminProducts />} />
										<Route path='orders' element={<AdminOrders />} />
										<Route path='orders/:id' element={<AdminOrderDetail />} />
										<Route path='income' element={<AdminIncome />} />
									</Route>
								</Route>
								<Route path=':category' element={<Home />} />
								<Route path=':category/:slug' element={<Product />} />
							</Route>
						</Routes>
					</BrowserRouter>
				</CatalogProvider>
			</CartProvider>
		</AuthProvider>
	</ThemeProvider>
);

export default App;
