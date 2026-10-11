namespace ECommerce.Application.Exceptions;

/// <summary>
/// Base type for expected, client-facing failures. The API maps each subtype to an
/// HTTP status code and an RFC 7807 problem response; anything else becomes a 500.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}
