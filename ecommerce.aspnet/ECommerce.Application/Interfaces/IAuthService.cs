using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Interfaces;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshRequest request);
    Task LogoutAsync(string refreshToken);
    Task<UserResponse> GetCurrentUserAsync(Guid userId);
}