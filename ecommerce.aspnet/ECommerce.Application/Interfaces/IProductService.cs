using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Product;

namespace ECommerce.Application.Interfaces;

public interface IProductService
{
    Task<PagedResponse<ProductResponse>> GetProductsAsync(string? search, int page, int pageSize);
    Task<ProductResponse?> GetProductByIdAsync(Guid id);
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, Guid userId);
    Task<ProductResponse?> UpdateProductAsync(Guid id, UpdateProductRequest request, Guid userId);
    Task<bool> DeleteProductAsync(Guid id, Guid userId);
}