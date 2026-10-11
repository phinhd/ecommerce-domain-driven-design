using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ECommerceDbContext context) : base(context)
    {
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(rt => rt.Token == tokenHash, cancellationToken);

    public Task<bool> IsJwtRevokedAsync(string jwtId, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(rt => rt.JwtId == jwtId && rt.IsRevoked, cancellationToken);

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await DbSet
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.Revoke();
    }
}
