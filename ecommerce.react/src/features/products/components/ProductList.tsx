import { useState } from 'react';
import { useProducts, useDeleteProduct } from '../hooks/useProducts';
import { useCreateOrder } from '../../orders/hooks/useOrders';
import { Button } from '../../../shared/components/Button';
import { useDebounce } from '../../../shared/hooks/useDebounce';
import { ConfirmModal } from '../../../shared/components/ConfirmModal';
import { useAuth } from '../../auth/hooks/useAuth';
import { useToast } from '../../../shared/components/Toast';

const PLACEHOLDER_IMAGE = 'https://placehold.co/400x400';

interface ProductListProps {
  mode: 'my-products' | 'store';
}

export const ProductList = ({ mode }: ProductListProps) => {
  const { user } = useAuth();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, 500);
  
  const mine = mode === 'my-products' ? true : undefined;
  const excludeMine = mode === 'store' ? true : undefined;

  const { data, isLoading } = useProducts({ page, pageSize: 10, search: debouncedSearch, mine, excludeMine });
  const deleteMutation = useDeleteProduct();

  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [orderId, setOrderId] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const createOrderMutation = useCreateOrder();
  const { showToast } = useToast();

  const handleDeleteClick = (id: string) => {
    setDeleteId(id);
  };

  const handleConfirmDelete = async () => {
    if (deleteId) {
      try {
        await deleteMutation.mutateAsync(deleteId);
        showToast('Product deleted successfully!', 'success');
        setDeleteId(null);
      } catch {
        // Global error handler will show toast
        setDeleteId(null);
      }
    }
  };

  const handleCancelDelete = () => {
    setDeleteId(null);
  };

  if (isLoading) return <div>Loading...</div>;

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-center gap-4">
        <div className="relative w-full sm:w-96">
          <input
            type="text"
            placeholder="Search products..."
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition-all"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <svg className="absolute left-3 top-2.5 h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
        {mode === 'my-products' && (
          <Button onClick={() => window.location.href = '/products/new'}>Create Product</Button>
        )}
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {data?.items.map((product) => (
          <div key={product.id} className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden hover:shadow-md transition-shadow flex flex-col justify-between">
            <div className="aspect-square w-full bg-gray-100 relative overflow-hidden">
              <img
                src={product.imageUrl?.trim() ? product.imageUrl : PLACEHOLDER_IMAGE}
                alt={product.name}
                className="w-full h-full object-cover"
                onError={(e) => {
                  (e.target as HTMLImageElement).src = PLACEHOLDER_IMAGE;
                }}
              />
            </div>
            <div className="p-5 flex-1 flex flex-col justify-between">
              <div>
                <h3 className="text-lg font-semibold text-gray-900 mb-1 line-clamp-1">{product.name}</h3>
                {product.description && (
                  <p className="text-gray-500 text-sm mb-3 line-clamp-2">{product.description}</p>
                )}
                <div className="flex items-center justify-between mb-4">
                  <p className="text-indigo-600 font-bold text-xl">${product.price}</p>
                  <p className="text-gray-500 text-sm">Qty: {product.quantity}</p>
                </div>
              </div>
              {mode === 'my-products' && user?.id === product.createdByUserId && (
                <div className="flex gap-2 mt-auto">
                  <Button variant="secondary" className="flex-1" onClick={() => window.location.href = `/products/${product.id}/edit`}>Edit</Button>
                  <Button variant="danger" className="flex-1" onClick={() => handleDeleteClick(product.id)}>Delete</Button>
                </div>
              )}
              {mode === 'store' && user?.id !== product.createdByUserId && (
                <div className="flex gap-2 mt-auto">
                  <Button variant="primary" className="flex-1" onClick={() => setOrderId(product.id)}>Order</Button>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      <ConfirmModal
        isOpen={deleteId !== null}
        title="Delete Product"
        message="Are you sure you want to delete this product? This action cannot be undone."
        onConfirm={handleConfirmDelete}
        onCancel={handleCancelDelete}
      />

      {orderId && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white p-6 rounded-lg shadow-xl w-96">
            <h2 className="text-lg font-bold mb-4">Order Product</h2>
            <label className="block text-sm font-medium text-gray-700 mb-2">Quantity</label>
            <input
              type="number"
              min="1"
              value={quantity}
              onChange={(e) => setQuantity(parseInt(e.target.value))}
              className="w-full p-2 border border-gray-300 rounded mb-4"
            />
            <div className="flex gap-2">
              <Button variant="secondary" className="flex-1" onClick={() => setOrderId(null)}>Cancel</Button>
              <Button variant="primary" className="flex-1" isLoading={createOrderMutation.isPending} onClick={async () => {
                try {
                  await createOrderMutation.mutateAsync({ productId: orderId, quantity });
                  setOrderId(null);
                  showToast('Product ordered successfully!', 'success');
                } catch {
                  // Global handler handles error toast
                }
              }}>Confirm</Button>
            </div>
          </div>
        </div>
      )}

      <div className="flex items-center justify-center gap-4 mt-8">
        <Button variant="secondary" disabled={page === 1} onClick={() => setPage(p => p - 1)}>Previous</Button>
        <span className="text-sm font-medium text-gray-600">Page {page} of {data?.totalPages || 1}</span>
        <Button variant="secondary" disabled={page === (data?.totalPages || 1)} onClick={() => setPage(p => p + 1)}>Next</Button>
      </div>
    </div>
  );
};