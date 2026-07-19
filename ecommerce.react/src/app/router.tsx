import React from 'react';
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
          { path: 'products', element: <ProductList /> },
          { path: 'products/new', element: <CreateProductPage /> },
          { path: '/', element: <Navigate to="/products" /> },
        ],
      },
    ],
  },
]);
