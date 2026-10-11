using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        string? keyword,
        int page,
        int pageSize,
        Guid? filterUserId = null,
        bool excludeUser = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically decrements stock with a single conditional UPDATE
    /// (<c>... SET quantity = quantity - n WHERE quantity &gt;= n</c>), so two
    /// concurrent orders can never oversell. Returns false when stock is insufficient.
    /// </summary>
    Task<bool> TryDecreaseStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}
