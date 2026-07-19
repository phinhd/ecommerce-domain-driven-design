using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Persistence;

namespace ECommerce.Infrastructure.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ECommerceDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _dbSet.FirstOrDefaultAsync(rt => rt.Token == tokenHash);
    }

    public async Task RevokeAllByUserIdAsync(Guid userId)
    {
        var tokensToRevoke = await _dbSet
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokensToRevoke)
        {
            token.IsRevoked = true;
        }

        _dbSet.UpdateRange(tokensToRevoke);
    }
}