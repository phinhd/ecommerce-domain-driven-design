using BCrypt.Net;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Infrastructure.Services;

public sealed class PasswordHasherService : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (Exception ex) when (ex is SaltParseException or BcryptAuthenticationException)
        {
            // A malformed hash is treated as a failed login, never as a server error.
            return false;
        }
    }
}
