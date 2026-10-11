namespace ECommerce.Application.Common;

/// <summary>Abstracts the clock so time-dependent rules (token expiry, timestamps) are testable.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
