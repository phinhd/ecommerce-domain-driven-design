using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Auth;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = null!;
}
