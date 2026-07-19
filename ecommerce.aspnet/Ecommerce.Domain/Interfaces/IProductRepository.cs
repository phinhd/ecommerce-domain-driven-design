using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(string? keyword, int page, int pageSize, Guid? filterUserId = null, bool excludeUser = false);
}
