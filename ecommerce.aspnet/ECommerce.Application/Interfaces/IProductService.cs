using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Product;

namespace ECommerce.Application.Interfaces;

public interface IProductService
{
    Task<PagedResponse<ProductResponse>> GetProductsAsync(ProductQuery query, Guid? currentUserId, CancellationToken cancellationToken = default);
    Task<ProductResponse> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
