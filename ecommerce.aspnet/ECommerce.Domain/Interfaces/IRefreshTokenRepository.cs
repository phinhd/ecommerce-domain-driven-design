using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> IsJwtRevokedAsync(string jwtId, CancellationToken cancellationToken = default);
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
