using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Ecommerce.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(IUnitOfWork unitOfWork, IJwtTokenService jwtTokenService, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existingUser != null) throw new InvalidOperationException("Email already exists.");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password)
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new UserResponse { Id = user.Id, Email = user.Email };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new Exceptions.UnauthorizedException("Invalid credentials.");

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenRaw = _jwtTokenService.GenerateRefreshToken();
        
        var expiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        var refreshToken = new RefreshToken
        {
            Token = HashToken(refreshTokenRaw),
            JwtId = GetJwtIdFromToken(accessToken),
            UserId = user.Id,
            AddedDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(expiryDays)
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse { AccessToken = accessToken, RefreshToken = refreshTokenRaw, ExpiresIn = 900 };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshRequest request)
    {
        var hashedToken = HashToken(request.RefreshToken);
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(hashedToken);

        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid token.");

        if (storedToken.IsUsed)
        {
            await _unitOfWork.RefreshTokens.RevokeAllByUserIdAsync(storedToken.UserId);
            await _unitOfWork.SaveChangesAsync();
            throw new UnauthorizedAccessException("Replay attack detected.");
        }

        storedToken.IsUsed = true;
        var user = await _unitOfWork.Users.GetByIdAsync(storedToken.UserId);
        if (user == null) throw new UnauthorizedAccessException("User not found.");

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshTokenRaw = _jwtTokenService.GenerateRefreshToken();

        var expiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
        var newRefreshToken = new RefreshToken
        {
            Token = HashToken(newRefreshTokenRaw),
            JwtId = GetJwtIdFromToken(newAccessToken),
            UserId = user.Id,
            AddedDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(expiryDays)
        };

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse { AccessToken = newAccessToken, RefreshToken = newRefreshTokenRaw, ExpiresIn = 900 };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hashedToken = HashToken(refreshToken);
        var storedToken = await _unitOfWork.RefreshTokens.GetByTokenHashAsync(hashedToken);
        if (storedToken != null)
        {
            storedToken.IsRevoked = true;
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<UserResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) throw new KeyNotFoundException("User not found.");
        return new UserResponse { Id = user.Id, Email = user.Email };
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private string GetJwtIdFromToken(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Id;
    }
}