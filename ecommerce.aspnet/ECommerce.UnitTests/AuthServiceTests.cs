using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.Shared.Enums;
using Microsoft.Extensions.Options;
using Moq;

namespace ECommerce.UnitTests;

public sealed class AuthServiceTests
{
    private readonly UnitOfWorkMock _uow = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly FakeClock _clock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns(() => new AccessToken("access-token", Guid.NewGuid().ToString(), _clock.UtcNow.AddMinutes(15)));
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");
        _hasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns<string>(p => $"hashed:{p}");

        var settings = Options.Create(new JwtSettings
        {
            SecretKey = new string('k', 32),
            Issuer = "issuer",
            Audience = "audience",
            DurationInMinutes = 15,
            RefreshTokenExpiryDays = 7
        });

        _sut = new AuthService(_uow.Object, _jwt.Object, _hasher.Object, _clock, settings);
    }

    [Fact]
    public async Task Register_CreatesActiveCustomer_WithNormalizedEmailAndHashedPassword()
    {
        User? saved = null;
        _uow.Users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => saved = u);

        var result = await _sut.RegisterAsync(new RegisterRequest { Email = " New@Example.com ", Password = "Password123!" });

        Assert.NotNull(saved);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("hashed:Password123!", saved.PasswordHash);
        Assert.Equal(UserTypeEnum.CUSTOMER, saved.Type);
        Assert.Equal(UserStatusEnum.ACTIVE, saved.Status);
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_ThrowsConflict_WhenEmailExists()
    {
        _uow.Users.Setup(r => r.GetByEmailAsync("taken@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = "taken@example.com", PasswordHash = "x" });

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.RegisterAsync(new RegisterRequest { Email = "Taken@Example.com", Password = "Password123!" }));
    }

    [Fact]
    public async Task Login_ReturnsTokenPair_AndStoresHashedRefreshToken()
    {
        var user = ActiveUser();
        _uow.Users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.VerifyPassword("Password123!", user.PasswordHash)).Returns(true);
        RefreshToken? stored = null;
        _uow.RefreshTokens.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((t, _) => stored = t);

        var result = await _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "Password123!" });

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        Assert.Equal(15 * 60, result.ExpiresIn);
        Assert.NotNull(stored);
        Assert.NotEqual("new-refresh-token", stored.Token); // only the hash is persisted
        Assert.Equal(AuthService.HashToken("new-refresh-token"), stored.Token);
        Assert.Equal(_clock.UtcNow.AddDays(7), stored.ExpiryDate);
    }

    [Fact]
    public async Task Login_Throws_WhenPasswordIsWrong()
    {
        var user = ActiveUser();
        _uow.Users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "wrong" }));
    }

    [Fact]
    public async Task Login_Throws_WhenUserDoesNotExist()
    {
        _uow.Users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.LoginAsync(new LoginRequest { Email = "ghost@example.com", Password = "Password123!" }));
    }

    [Fact]
    public async Task Login_Throws_WhenUserIsInactive()
    {
        var user = ActiveUser();
        user.Status = UserStatusEnum.INACTIVE;
        _uow.Users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.LoginAsync(new LoginRequest { Email = user.Email, Password = "Password123!" }));
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndMarksOldOneUsed()
    {
        var user = ActiveUser();
        var old = StoredToken(user.Id, "old-refresh-token");
        _uow.RefreshTokens.Setup(r => r.GetByTokenHashAsync(AuthService.HashToken("old-refresh-token"), It.IsAny<CancellationToken>())).ReturnsAsync(old);
        _uow.Users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.RefreshTokenAsync(new RefreshRequest { RefreshToken = "old-refresh-token" });

        Assert.True(old.IsUsed);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        _uow.RefreshTokens.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithReusedToken_RevokesAllSessions()
    {
        var userId = Guid.NewGuid();
        var reused = StoredToken(userId, "stolen-token");
        reused.IsUsed = true;
        _uow.RefreshTokens.Setup(r => r.GetByTokenHashAsync(AuthService.HashToken("stolen-token"), It.IsAny<CancellationToken>())).ReturnsAsync(reused);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.RefreshTokenAsync(new RefreshRequest { RefreshToken = "stolen-token" }));

        _uow.RefreshTokens.Verify(r => r.RevokeAllByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _jwt.Verify(j => j.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_Throws()
    {
        var expired = StoredToken(Guid.NewGuid(), "expired-token");
        expired.ExpiryDate = _clock.UtcNow.AddSeconds(-1);
        _uow.RefreshTokens.Setup(r => r.GetByTokenHashAsync(AuthService.HashToken("expired-token"), It.IsAny<CancellationToken>())).ReturnsAsync(expired);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.RefreshTokenAsync(new RefreshRequest { RefreshToken = "expired-token" }));
    }

    [Fact]
    public async Task Logout_RevokesToken()
    {
        var token = StoredToken(Guid.NewGuid(), "session-token");
        _uow.RefreshTokens.Setup(r => r.GetByTokenHashAsync(AuthService.HashToken("session-token"), It.IsAny<CancellationToken>())).ReturnsAsync(token);

        await _sut.LogoutAsync("session-token");

        Assert.True(token.IsRevoked);
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static User ActiveUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        PasswordHash = "hashed:Password123!",
        Type = UserTypeEnum.CUSTOMER,
        Status = UserStatusEnum.ACTIVE
    };

    private RefreshToken StoredToken(Guid userId, string raw) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Token = AuthService.HashToken(raw),
        JwtId = Guid.NewGuid().ToString(),
        AddedDate = _clock.UtcNow.AddDays(-1),
        ExpiryDate = _clock.UtcNow.AddDays(6)
    };
}
