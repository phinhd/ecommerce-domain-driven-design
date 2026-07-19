using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task RevokeAllByUserIdAsync(Guid userId);
}