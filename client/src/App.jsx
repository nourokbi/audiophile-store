import React from 'react';

import Home from './pages/Home';
import Category from './pages/Category';
import Checkout from './pages/Checkout';
import Product from './pages/Product';

import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';

import data from './data/data.json';

const App = () => {
	const [categoryData, setCategoryData] = React.useState({
		headphones: [],
		speakers: [],
		earphones: [],
	});

	React.useEffect(() => {
		const categorizedData = {
			headphones: data.filter((item) => item.category === 'headphones'),
			speakers: data.filter((item) => item.category === 'speakers'),
			earphones: data.filter((item) => item.category === 'earphones'),
		};
		setCategoryData(categorizedData);
	}, []);

	const categories = {
		headphones: {
			title: 'Headphones',
			products: categoryData.headphones,
		},
		speakers: {
			title: 'Speakers',
			products: categoryData.speakers,
		},
		earphones: {
			title: 'Earphones',
			products: categoryData.earphones,
		},
	};

	const [quantityToAdd, setQuantityToAdd] = React.useState(1);

	const increaseCount = () => {
		setQuantityToAdd((prev) => prev + 1);
	};

	const decreaseCount = () => {
		setQuantityToAdd((prev) => {
			return prev - 1 < 1 ? prev : prev - 1;
		});
	};

	const updateQuantity = (index, newQuantity) => {
		const updatedCart = [...cartData];
		updatedCart[index].quantity = newQuantity;
		setCartData(updatedCart);
	};

	const [cartData, setCartData] = React.useState([]);
	const [totalPrice, setTotalPrice] = React.useState(0);

	const addToCart = (product) => {
		const existingProductIndex = cartData.findIndex(
			(item) => item.name === product.name,
		);

		const newQuantity =
			existingProductIndex !== -1
				? cartData[existingProductIndex].quantity + quantityToAdd
				: quantityToAdd;

		if (existingProductIndex !== -1) {
			const updatedCartData = [...cartData];
			updatedCartData[existingProductIndex].quantity = newQuantity;
			setCartData(updatedCartData);
		} else {
			product.quantity = newQuantity;
			setCartData([...cartData, product]);
		}

		setTotalPrice((prevPrice) => prevPrice + product.price * quantityToAdd);
	};

	const removeFromCart = (indexToRemove) => {
		const updatedCart = cartData.filter((_, index) => index !== indexToRemove);
		setCartData(updatedCart);
		setTotalPrice((prevPrice) => prevPrice - cartData[indexToRemove].price);
	};

	const clearCart = () => {
		setCartData([]);
		setTotalPrice(0);
	};

	return (
		<>
			<BrowserRouter>
				<Routes>
					<Route
						element={
							<Layout
								cart={cartData}
								clearCart={clearCart}
								totalPrice={totalPrice}
								setTotalPrice={setTotalPrice}
								updateQuantity={updateQuantity}
								removeFromCart={removeFromCart}
							/>
						}
					>
						<Route path='/' element={<Home />} />
						<Route
							path=':category'
							element={<Category categories={categories} />}
						/>
						<Route
							path=':category/:slug'
							element={
								<Product
									quantityToAdd={quantityToAdd}
									increase={increaseCount}
									decrease={decreaseCount}
									addToCart={addToCart}
								/>
							}
						/>
						<Route
							path='/checkout'
							element={<Checkout cart={cartData} totalPrice={totalPrice} />}
						/>
					</Route>
				</Routes>
			</BrowserRouter>
		</>
	);
};

export default App;
