using ECommerce.Domain.Interfaces;
using BCrypt.Net;

namespace ECommerce.Infrastructure.Services;

public class PasswordHasherService : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BcryptAuthenticationException)
        {
            return false;
        }
    }
}