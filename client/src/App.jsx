import { BrowserRouter, Routes, Route } from 'react-router-dom';

import Home from './pages/Home';
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

// Who you are first, then your cart, then the catalogue
const App = () => (
	<AuthProvider>
		<CartProvider>
			<CatalogProvider>
				<BrowserRouter>
					<Routes>
						<Route element={<Layout />}>
							<Route path='/' element={<Home />} />
							<Route path='/login' element={<AuthPage mode='login' />} />
							<Route path='/register' element={<AuthPage mode='register' />} />
							<Route element={<RequireAuth />}>
								<Route path='/checkout' element={<Checkout />} />
								<Route path='/orders' element={<Orders />} />
								<Route path='/orders/:id' element={<OrderDetail />} />
							</Route>
							<Route path=':category' element={<Home />} />
							<Route path=':category/:slug' element={<Product />} />
						</Route>
					</Routes>
				</BrowserRouter>
			</CatalogProvider>
		</CartProvider>
	</AuthProvider>
);

export default App;
