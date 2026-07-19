using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.Application.DTOs.Order;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ECommerce.IntegrationTests;

public class OrderControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("ecommerce_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"JwtSettings:SecretKey", "super-secret-key-that-is-at-least-16-characters-long"},
                    {"JwtSettings:Issuer", "test-issuer"},
                    {"JwtSettings:Audience", "test-audience"}
                }!);
            });
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ECommerceDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<ECommerceDbContext>(options => options.UseNpgsql(_dbContainer.GetConnectionString()));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
                db.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    private string GenerateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-key-that-is-at-least-16-characters-long"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var token = new JwtSecurityToken("test-issuer", "test-audience", claims, expires: DateTime.Now.AddHours(1), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task DisposeAsync() => await _dbContainer.StopAsync();

    [Fact]
    public async Task CreateOrder_ShouldReduceStockAndCreateOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        
        var product = new Product { Name = "Test Product", Price = 100m, Quantity = 5, CreatedByUserId = sellerId };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(userId));

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", new { productId = product.Id, quantity = 2 });
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var updatedProduct = await db.Products.FindAsync(product.Id);
        Assert.Equal(3, updatedProduct!.Quantity);
        
        var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.UserId == userId);
        Assert.NotNull(order);
        Assert.Single(order.OrderItems);
        Assert.Equal(2, order.OrderItems.First().Quantity);
    }
}