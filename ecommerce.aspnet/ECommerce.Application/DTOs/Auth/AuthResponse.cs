namespace ECommerce.Application.DTOs.Auth;

public sealed class AuthResponse
{
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;

    /// <summary>Access token lifetime in seconds.</summary>
    public int ExpiresIn { get; init; }
}
