namespace ECommerce.Application.DTOs.Auth;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
}