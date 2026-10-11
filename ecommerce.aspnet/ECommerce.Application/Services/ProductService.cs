using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Product;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Services;

public sealed class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ProductService(IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<PagedResponse<ProductResponse>> GetProductsAsync(ProductQuery query, Guid? currentUserId, CancellationToken cancellationToken = default)
    {
        if (query.Mine && query.ExcludeMine)
            throw new BadRequestException("'mine' and 'excludeMine' cannot be combined.");

        if ((query.Mine || query.ExcludeMine) && currentUserId is null)
            throw new UnauthorizedException("Sign in to filter by your own products.");

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, Pagination.MaxPageSize);
        var filterUserId = query.Mine || query.ExcludeMine ? currentUserId : null;

        var (items, totalCount) = await _unitOfWork.Products.SearchAsync(
            query.Search, page, pageSize, filterUserId, excludeUser: query.ExcludeMine, cancellationToken);

        return new PagedResponse<ProductResponse>(items.Select(MapToResponse).ToList(), totalCount, page, pageSize);
    }

    public async Task<ProductResponse> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => MapToResponse(await GetActiveProductAsync(id, cancellationToken));

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var product = Product.Create(userId, request.Name, request.Description, request.Price, request.Quantity, request.ImageUrl, _clock.UtcNow);

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(product);
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var product = await GetActiveProductAsync(id, cancellationToken);
        if (!product.IsOwnedBy(userId))
            throw new ForbiddenException("You do not have permission to modify this product.");

        product.UpdateDetails(request.Name, request.Description, request.Price, request.Quantity, request.ImageUrl, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(product);
    }

    public async Task DeleteProductAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var product = await GetActiveProductAsync(id, cancellationToken);
        if (!product.IsOwnedBy(userId))
            throw new ForbiddenException("You do not have permission to delete this product.");

        product.SoftDelete(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Product> GetActiveProductAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        return product is null || product.IsDeleted()
            ? throw new NotFoundException("Product not found.")
            : product;
    }

    private static ProductResponse MapToResponse(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        Quantity = product.Quantity,
        ImageUrl = product.ImageUrl,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt,
        CreatedByUserId = product.CreatedByUserId
    };
}
