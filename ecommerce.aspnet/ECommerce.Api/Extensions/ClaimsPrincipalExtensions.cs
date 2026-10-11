using System.Security.Claims;
using ECommerce.Application.Exceptions;

namespace ECommerce.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The JWT "sub" claim is mapped to <see cref="ClaimTypes.NameIdentifier"/> by the bearer handler.
    /// </summary>
    public static Guid? FindUserId(this ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static Guid GetUserId(this ClaimsPrincipal principal)
        => principal.FindUserId() ?? throw new UnauthorizedException("The access token does not contain a valid user id.");
}
