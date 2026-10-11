using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}