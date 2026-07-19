import React, { useState } from 'react';
import { useProducts, useDeleteProduct } from '../hooks/useProducts';
import { Button } from '../../../shared/components/Button';
import { useDebounce } from '../../../shared/hooks/useDebounce';
import { ConfirmModal } from '../../../shared/components/ConfirmModal';

const PLACEHOLDER_IMAGE = 'https://placehold.co/400x400';

export const ProductList = () => {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, 500);
  const { data, isLoading } = useProducts({ page, pageSize: 10, search: debouncedSearch });
  const deleteMutation = useDeleteProduct();

  const [deleteId, setDeleteId] = useState<string | null>(null);

  const handleDeleteClick = (id: string) => {
    setDeleteId(id);
  };

  const handleConfirmDelete = async () => {
    if (deleteId) {
      await deleteMutation.mutateAsync(deleteId);
      setDeleteId(null);
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
        <Button onClick={() => window.location.href = '/products/new'}>Create Product</Button>
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
              <div className="flex gap-2 mt-auto">
                <Button variant="secondary" className="flex-1" onClick={() => window.location.href = `/products/${product.id}/edit`}>Edit</Button>
                <Button variant="danger" className="flex-1" onClick={() => handleDeleteClick(product.id)}>Delete</Button>
              </div>
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

      <div className="flex items-center justify-center gap-4 mt-8">
        <Button variant="secondary" disabled={page === 1} onClick={() => setPage(p => p - 1)}>Previous</Button>
        <span className="text-sm font-medium text-gray-600">Page {page} of {data?.totalPages || 1}</span>
        <Button variant="secondary" disabled={page === (data?.totalPages || 1)} onClick={() => setPage(p => p + 1)}>Next</Button>
      </div>
    </div>
  );
};