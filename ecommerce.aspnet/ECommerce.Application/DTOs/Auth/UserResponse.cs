namespace ECommerce.Application.DTOs.Auth;

public sealed class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
}