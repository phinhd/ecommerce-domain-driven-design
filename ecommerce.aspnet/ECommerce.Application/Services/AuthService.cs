using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.Shared.Enums;
using Microsoft.Extensions.Options;

namespace ECommerce.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IDateTimeProvider clock,
        IOptions<JwtSettings> jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = User.NormalizeEmail(request.Email);

        if (await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken) is not null)
            throw new ConflictException("Email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Type = UserTypeEnum.CUSTOMER,
            Status = UserStatusEnum.ACTIVE,
            CreatedAt = _clock.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToUserResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken);

        // One message for every failure so the endpoint does not reveal which emails exist.
        if (user is null || !user.IsActive() || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials.");

        var response = await IssueTokensAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }

    /// <summary>
    /// Refresh token rotation with reuse detection: every refresh token works once.
    /// Presenting an already-used token means it leaked, so the whole session family is revoked.
    /// </summary>
    public async Task<AuthResponse> RefreshTokenAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(HashToken(request.RefreshToken), cancellationToken);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiryDate <= now)
            throw new UnauthorizedException("Invalid refresh token.");

        if (storedToken.IsUsed)
        {
            await _unitOfWork.RefreshTokens.RevokeAllByUserIdAsync(storedToken.UserId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token reuse detected. All sessions have been revoked.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null || !user.IsActive())
            throw new UnauthorizedException("Invalid refresh token.");

        storedToken.MarkUsed();
        var response = await IssueTokensAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(HashToken(refreshToken), cancellationToken);
        if (storedToken is null || storedToken.IsRevoked)
            return;

        storedToken.Revoke();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return ToUserResponse(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();

        await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashToken(rawRefreshToken),
            JwtId = accessToken.JwtId,
            UserId = user.Id,
            AddedDate = now,
            CreatedAt = now,
            ExpiryDate = now.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        }, cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            RefreshToken = rawRefreshToken,
            ExpiresIn = _jwtSettings.DurationInMinutes * 60
        };
    }

    /// <summary>Refresh tokens are stored as SHA-256 hashes, so a database leak does not leak usable tokens.</summary>
    internal static string HashToken(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static UserResponse ToUserResponse(User user) => new() { Id = user.Id, Email = user.Email };
}
