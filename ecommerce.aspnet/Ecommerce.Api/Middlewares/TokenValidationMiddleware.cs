using System.Security.Claims;
using ECommerce.Domain.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ecommerce.Api.Middlewares;

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        // Only run for authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            
            if (string.IsNullOrEmpty(jti))
            {
                jti = context.User.FindFirst("jti")?.Value;
            }

            if (!string.IsNullOrEmpty(jti))
            {
                // Check if the token has been revoked in database
                var tokens = await unitOfWork.RefreshTokens.FindAsync(rt => rt.JwtId == jti && rt.IsRevoked);
                var isRevoked = tokens.Any();

                if (isRevoked)
                {
                    // Token is revoked, return 401 Unauthorized
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { Message = "Token has been revoked." });
                    return; // Stop pipeline
                }
            }
        }

        await _next(context);
    }
}