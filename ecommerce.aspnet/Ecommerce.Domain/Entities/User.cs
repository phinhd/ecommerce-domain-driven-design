using ECommerce.Domain.Shared.Enums;

namespace ECommerce.Domain.Entities;

public class User : BaseClass
{
    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? CountryCode { get; set; }

    public string? Username { get; set; }

    public bool? IsDeleted { get; set; }

    public UserTypeEnum Type { get; set; }
    public UserStatusEnum Status { get; set; }

    public bool? IsLoggedIn { get; set; }

    public string? TimeZone { get; set; }

    public string? PhoneCode { get; set; }

    public bool? IsFirstLogin { get; set; }
}
