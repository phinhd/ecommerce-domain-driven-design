using System.Security.Claims;
using ECommerce.Domain.Entities;

namespace Ecommerce.Domain.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateAccessToken(string token);
}