using System.Security.Claims;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var userResponse = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(GetMe), new { id = userResponse.Id }, userResponse);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var authResponse = await _authService.LoginAsync(request);
            return Ok(authResponse);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
    {
        try
        {
            var authResponse = await _authService.RefreshTokenAsync(request);
            return Ok(authResponse);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetCurrentUserAsync(userId);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = "User not found." });
        }
    }
}