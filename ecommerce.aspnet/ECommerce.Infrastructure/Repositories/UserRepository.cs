using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ECommerceDbContext context) : base(context)
    {
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = User.NormalizeEmail(email);
        return DbSet.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }
}
