using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.Application.DTOs.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Runs the real API (real migrations, real middleware) against a disposable PostgreSQL container.
/// The container is shared by every test class in the collection.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Password = "Password123!";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("ecommerce_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _database.StartAsync();

    public new async Task DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _database.GetConnectionString());
        builder.UseSetting("JwtSettings:SecretKey", "integration-tests-secret-key-0123456789abcdef");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Seed:SampleData", "false");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "10000");
    }

    /// <summary>Registers a fresh user and returns a client authenticated as that user.</summary>
    public async Task<(HttpClient Client, AuthResponse Tokens)> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = Password })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = Password });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return (client, tokens);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
