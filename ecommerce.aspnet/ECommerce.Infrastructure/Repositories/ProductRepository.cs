using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Persistence;

namespace ECommerce.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ECommerceDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(string name)
    {
        return await _dbSet
            .Where(p => p.Name.Contains(name))
            .ToListAsync();
    }
}