using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Auth;

public sealed class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}