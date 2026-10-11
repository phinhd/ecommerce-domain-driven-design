namespace ECommerce.Domain.Entities;

public class RefreshToken : BaseClass
{
    /// <summary>SHA-256 hash of the raw token. The raw value is only ever sent to the client.</summary>
    public string Token { get; set; } = null!;
    public string JwtId { get; set; } = null!;
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime AddedDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    public Guid UserId { get; set; }

    public virtual User User { get; set; } = null!;

    public bool IsActive(DateTime nowUtc) => !IsUsed && !IsRevoked && ExpiryDate > nowUtc;

    public void MarkUsed() => IsUsed = true;

    public void Revoke() => IsRevoked = true;
}
