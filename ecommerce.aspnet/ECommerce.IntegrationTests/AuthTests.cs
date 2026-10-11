using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthTests
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_ThenMe_ReturnsCurrentUser()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync();

        var me = await client.GetFromJsonAsync<UserResponse>("/api/auth/me");

        Assert.NotNull(me);
        Assert.StartsWith("user-", me.Email, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest { Email = $"dup-{Guid.NewGuid():N}@example.com", Password = ApiFactory.Password };
        (await client.PostAsJsonAsync("/api/auth/register", request)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Email already exists.", problem!.Detail);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = $"wrong-{Guid.NewGuid():N}@example.com";
        (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = ApiFactory.Password })).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesTokens_AndReuseRevokesTheSession()
    {
        var (client, tokens) = await _factory.CreateAuthenticatedClientAsync();

        var first = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest { RefreshToken = tokens.RefreshToken });
        first.EnsureSuccessStatusCode();
        var rotated = (await first.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.NotEqual(tokens.RefreshToken, rotated.RefreshToken);

        // Replaying the old refresh token is treated as theft: rejected, and the new session is revoked too.
        var replay = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest { RefreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rotated.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(me)).StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesAccessToken()
    {
        var (client, tokens) = await _factory.CreateAuthenticatedClientAsync();

        var logout = await client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest { RefreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Health_IsHealthy()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
