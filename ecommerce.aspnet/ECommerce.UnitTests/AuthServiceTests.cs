using System.Security.Claims;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ECommerce.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.RefreshTokens).Returns(_mockRefreshTokenRepository.Object);

        _mockConfiguration.Setup(c => c["JwtSettings:RefreshTokenExpiryDays"]).Returns("7");

        _authService = new AuthService(
            _mockUnitOfWork.Object,
            _mockJwtTokenService.Object,
            _mockPasswordHasher.Object,
            _mockConfiguration.Object
        );
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "Password123!" };
        var user = new User { Id = Guid.NewGuid(), Email = request.Email, PasswordHash = "hashed_password" };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(h => h.VerifyPassword(request.Password, user.PasswordHash)).Returns(true);
        _mockJwtTokenService.Setup(s => s.GenerateAccessToken(user)).Returns(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().CreateEncodedJwt(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor()));
        _mockJwtTokenService.Setup(s => s.GenerateRefreshToken()).Returns("refresh_token");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
        Assert.Equal(900, result.ExpiresIn);
        
        _mockRefreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "WrongPassword" };
        var user = new User { Id = Guid.NewGuid(), Email = request.Email, PasswordHash = "hashed_password" };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockPasswordHasher.Setup(h => h.VerifyPassword(request.Password, user.PasswordHash)).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request));
    }
    
    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest { Email = "notfound@example.com", Password = "Password123!" };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(request.Email)).ReturnsAsync((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request));
    }
}