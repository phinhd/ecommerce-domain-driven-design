import { useNavigate } from 'react-router-dom';
import { ProductForm } from '../components/ProductForm';
import { useCreateProduct } from '../hooks/useProducts';
import { useToast } from '../../../shared/components/Toast';
import type { Product } from '../types';

export const CreateProductPage = () => {
  const navigate = useNavigate();
  const createMutation = useCreateProduct();
  const { showToast } = useToast();

  const handleSubmit = async (data: Omit<Product, 'id' | 'createdByUserId'>) => {
    try {
      await createMutation.mutateAsync(data);
      showToast('Thêm sản phẩm thành công!', 'success');
      navigate('/my-products');
    } catch (error) {
      console.error('Failed to create product', error);
    }
  };

  return (
    <div className="max-w-2xl mx-auto py-8">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Create New Product</h1>
      <ProductForm onSubmit={handleSubmit} isLoading={createMutation.isPending} />
    </div>
  );
};