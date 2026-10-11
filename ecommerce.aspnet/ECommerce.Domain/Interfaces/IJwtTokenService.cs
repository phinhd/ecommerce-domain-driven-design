using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

/// <summary>A signed access token plus the metadata needed to link a refresh token to it.</summary>
public sealed record AccessToken(string Token, string JwtId, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessToken GenerateAccessToken(User user);

    /// <summary>Returns a cryptographically random, URL-safe refresh token (raw value, not hashed).</summary>
    string GenerateRefreshToken();
}
