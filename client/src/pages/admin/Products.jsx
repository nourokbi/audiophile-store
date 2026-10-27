import React from 'react';
import { Plus } from 'lucide-react';

import AdminPageHeader from '../../components/admin/AdminPageHeader';
import NumberDialog from '../../components/admin/NumberDialog';
import ProductDialog from '../../components/admin/ProductDialog';
import ProductFilters from '../../components/admin/ProductFilters';
import ProductsTable from '../../components/admin/ProductsTable';
import { useAdminProducts } from '../../hooks/useAdminProducts';
import { adminApi } from '../../lib/api/admin';
import { formatPrice } from '../../lib/format';
import { LOW_STOCK_LIMIT } from '../../lib/productHelpers';

const NEW_PRODUCT_ID = 'new'; // marks the add product form as saving, it has no id yet

// The admin products page and the one place with its logic, the components under components/admin only draw what they are given
const Products = () => {
	const productList = useAdminProducts();
	const [openDialog, setOpenDialog] = React.useState(null); // { type: 'add' | 'edit' | 'restock' | 'price', product }
	const [savingProductId, setSavingProductId] = React.useState(null);
	const [saveError, setSaveError] = React.useState('');

	const showDialog = (type, product) => {
		setSaveError('');
		setOpenDialog({ type, product });
	};
	const closeDialog = () => setOpenDialog(null);

	// Every change goes through here: send the request, then hand the product the server answered to applyResult
	const saveChange = async (productId, sendRequest, applyResult) => {
		setSavingProductId(productId);
		setSaveError('');
		try {
			applyResult(await sendRequest());
			setOpenDialog(null);
		} catch (error) {
			setSaveError(error.message);
		} finally {
			setSavingProductId(null);
		}
	};

	const toggleProductAvailability = (product) =>
		saveChange(product.id, () => adminApi.setAvailability(product.id, !product.isActive), productList.replaceProduct);
	const restockProduct = (product, quantity) =>
		saveChange(product.id, () => adminApi.restock(product.id, quantity), productList.replaceProduct);
	const changeProductPrice = (product, price) =>
		saveChange(product.id, () => adminApi.changePrice(product.id, price), productList.replaceProduct);
	const editProduct = (product, productRequest) =>
		saveChange(product.id, () => adminApi.updateProduct(product.id, productRequest), productList.replaceProduct);
	const createProduct = (productRequest) =>
		saveChange(NEW_PRODUCT_ID, () => adminApi.createProduct(productRequest), productList.reloadFromFirstPage);

	const dialogProduct = openDialog?.product;

	return (
		<div className='flex flex-col gap-6'>
			<AdminPageHeader title='Products'>
				<button onClick={() => showDialog('add')} className='btn-primary'>
					<Plus size={16} /> Add product
				</button>
			</AdminPageHeader>

			<ProductFilters
				searchText={productList.searchText}
				onSearchTextChange={productList.setSearchText}
				filters={productList.filters}
				onFilterChange={productList.setFilter}
				lowStockLimit={LOW_STOCK_LIMIT}
				hasFilters={productList.hasFilters}
				onClearFilters={productList.clearFilters}
			/>

			<ProductsTable
				products={productList.products}
				lowStockLimit={LOW_STOCK_LIMIT}
				savingProductId={savingProductId}
				errorMessage={openDialog ? '' : saveError || productList.loadError}
				totalCount={productList.totalCount}
				hasFilters={productList.hasFilters}
				currentPage={productList.filters.page}
				totalPages={productList.totalPages}
				onPageChange={(pageNumber) => productList.setFilter('page', pageNumber)}
				onToggleAvailability={toggleProductAvailability}
				onEditClick={(product) => showDialog('edit', product)}
				onRestockClick={(product) => showDialog('restock', product)}
				onPriceClick={(product) => showDialog('price', product)}
			/>

			{openDialog?.type === 'add' && (
				<ProductDialog
					isSaving={savingProductId === NEW_PRODUCT_ID}
					errorMessage={saveError}
					onClose={closeDialog}
					onSave={createProduct}
				/>
			)}

			{openDialog?.type === 'edit' && (
				<ProductDialog
					product={dialogProduct}
					isSaving={savingProductId === dialogProduct.id}
					errorMessage={saveError}
					onClose={closeDialog}
					onSave={(productRequest) => editProduct(dialogProduct, productRequest)}
				/>
			)}

			{openDialog?.type === 'restock' && (
				<NumberDialog
					title={`Restock ${dialogProduct.name}`}
					label='Units to add'
					initialValue={10}
					minimum={1}
					isSaving={savingProductId === dialogProduct.id}
					errorMessage={saveError}
					describeChange={(quantity) => `Stock goes from ${dialogProduct.stock} to ${dialogProduct.stock + quantity}`}
					onClose={closeDialog}
					onSave={(quantity) => restockProduct(dialogProduct, quantity)}
				/>
			)}

			{openDialog?.type === 'price' && (
				<NumberDialog
					title={`Price of ${dialogProduct.name}`}
					label='New price in EGP'
					initialValue={dialogProduct.price}
					minimum={1}
					isSaving={savingProductId === dialogProduct.id}
					errorMessage={saveError}
					describeChange={(price) => `Was ${formatPrice(dialogProduct.price)}, becomes ${formatPrice(price)}`}
					onClose={closeDialog}
					onSave={(price) => changeProductPrice(dialogProduct, price)}
				/>
			)}
		</div>
	);
};

export default Products;
