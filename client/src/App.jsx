import { BrowserRouter, Routes, Route } from 'react-router-dom';

import Home from './pages/Home';
import Checkout from './pages/Checkout';
import Product from './pages/Product';
import AuthPage from './pages/AuthPage';
import Orders from './pages/Orders';
import OrderDetail from './pages/OrderDetail';
import Settings from './pages/Settings';
import AdminDashboard from './pages/AdminDashboard';
import Layout from './components/Layout';
import RequireAuth from './components/RequireAuth';
import { AuthProvider } from './context/AuthContext';
import { CartProvider } from './context/CartContext';
import { CatalogProvider } from './context/CatalogContext';
import { ThemeProvider } from './context/ThemeContext';

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
								<Route path='/login' element={<AuthPage mode='login' />} />
								<Route path='/register' element={<AuthPage mode='register' />} />
								<Route element={<RequireAuth />}>
									<Route path='/checkout' element={<Checkout />} />
									<Route path='/orders' element={<Orders />} />
									<Route path='/orders/:id' element={<OrderDetail />} />
									<Route path='/settings' element={<Settings />} />
								</Route>
								<Route element={<RequireAuth role='Admin' />}>
									<Route path='/admin' element={<AdminDashboard />} />
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
