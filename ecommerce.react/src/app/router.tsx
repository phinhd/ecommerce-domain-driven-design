import { createBrowserRouter, Navigate } from 'react-router-dom';
import { LoginForm } from '../features/auth/components/LoginForm';
import { RegisterForm } from '../features/auth/components/RegisterForm';
import { ProductList } from '../features/products/components/ProductList';
import { CreateProductPage } from '../features/products/pages/CreateProductPage';
import { ProtectedRoute } from './ProtectedRoute';
import { Layout } from './Layout';

export const router = createBrowserRouter([
  { path: '/login', element: <LoginForm /> },
  { path: '/register', element: <RegisterForm /> },
  {
    path: '/',
    element: <Layout />,
    children: [
      {
        element: <ProtectedRoute />,
        children: [
          { path: 'products', element: <Navigate to="/store" /> },
          { path: 'store', element: <ProductList mode="store" /> },
          { path: 'my-products', element: <ProductList mode="my-products" /> },
          { path: 'products/new', element: <CreateProductPage /> },
          { path: '/', element: <Navigate to="/store" /> },
        ],
      },
    ],
  },
]);
