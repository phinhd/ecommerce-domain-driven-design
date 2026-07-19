using System.Net.Http.Json;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Product;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ECommerce.IntegrationTests;

public class ProductControllerTests : IAsyncLifetime
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
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ECommerceDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ECommerceDbContext>(options =>
                {
                    options.UseNpgsql(_dbContainer.GetConnectionString());
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
                db.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }

    [Fact]
    public async Task GetProducts_ShouldReturnSeededProducts()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
        
        var product1 = new Product { Name = "Test Product 1", Description = "Desc 1", Price = 10m, Quantity = 10 };
        var product2 = new Product { Name = "Test Product 2", Description = "Desc 2", Price = 20m, Quantity = 20 };
        
        db.Products.AddRange(product1, product2);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();

        // Assert
        Assert.NotNull(pagedResponse);
        var productList = pagedResponse.Items.ToList();
        Assert.True(productList.Count >= 2);
        Assert.Contains(productList, p => p.Name == "Test Product 1");
        Assert.Contains(productList, p => p.Name == "Test Product 2");
    }
}