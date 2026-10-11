using System.IdentityModel.Tokens.Jwt;
using ECommerce.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Middlewares;

/// <summary>
/// JWTs are stateless, so a stolen access token would stay valid until it expires. This middleware
/// rejects tokens whose session (refresh token family) was revoked by logout or reuse detection.
/// </summary>
public sealed class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork, IProblemDetailsService problemDetails)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jti) && await unitOfWork.RefreshTokens.IsJwtRevokedAsync(jti, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "Unauthorized",
                        Detail = "Token has been revoked."
                    }
                });
                return;
            }
        }

        await _next(context);
    }
}
