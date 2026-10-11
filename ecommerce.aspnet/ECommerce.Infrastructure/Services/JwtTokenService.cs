using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _clock;

    public JwtTokenService(IOptions<JwtSettings> settings, IDateTimeProvider clock)
    {
        _settings = settings.Value;
        _clock = clock;
    }

    public AccessToken GenerateAccessToken(User user)
    {
        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_settings.DurationInMinutes);
        var jwtId = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new("type", user.Type.ToString()),
            new("status", user.Status.ToString())
        };

        if (!string.IsNullOrEmpty(user.Username))
            claims.Add(new Claim("username", user.Username));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        return new AccessToken(handler.WriteToken(handler.CreateToken(descriptor)), jwtId, expires);
    }

    public string GenerateRefreshToken()
        => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
}
