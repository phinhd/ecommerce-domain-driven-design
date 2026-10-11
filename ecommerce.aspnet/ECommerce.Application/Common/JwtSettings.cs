using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.Common;

/// <summary>Bound from the "JwtSettings" section and validated on startup.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>HMAC-SHA256 key, at least 32 characters (256 bits).</summary>
    [Required, MinLength(32)]
    public string SecretKey { get; set; } = null!;

    [Required]
    public string Issuer { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;

    [Range(1, 24 * 60)]
    public int DurationInMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
