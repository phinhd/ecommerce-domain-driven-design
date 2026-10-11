using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ECommerceDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        string? keyword,
        int page,
        int pageSize,
        Guid? filterUserId = null,
        bool excludeUser = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(p => p.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // ILIKE keeps the search case-insensitive and lets PostgreSQL use a trigram index if one is added.
            var pattern = $"%{EscapeLikePattern(keyword.Trim())}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern, "\\"));
        }

        if (filterUserId.HasValue)
        {
            query = excludeUser
                ? query.Where(p => p.CreatedByUserId != filterUserId.Value)
                : query.Where(p => p.CreatedByUserId == filterUserId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> TryDecreaseStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        var affected = await DbSet
            .Where(p => p.Id == productId && p.DeletedAt == null && p.Quantity >= quantity)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Quantity, p => p.Quantity - quantity), cancellationToken);

        return affected == 1;
    }

    private static string EscapeLikePattern(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
}
