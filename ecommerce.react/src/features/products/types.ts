export interface Product {
  id: string;
  name: string;
  description?: string;
  price: number;
  quantity: number;
  imageUrl?: string;
  createdAt?: string;
  updatedAt?: string;
  createdByUserId: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}