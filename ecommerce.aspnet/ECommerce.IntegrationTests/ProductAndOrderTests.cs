using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Order;
using ECommerce.Application.DTOs.Product;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ProductAndOrderTests
{
    private readonly ApiFactory _factory;

    public ProductAndOrderTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateProduct_ThenSearchCaseInsensitively_FindsIt()
    {
        var (seller, _) = await _factory.CreateAuthenticatedClientAsync();
        var name = $"Mechanical Keyboard {Guid.NewGuid():N}";
        (await seller.PostAsJsonAsync("/api/products", NewProduct(name, quantity: 3))).EnsureSuccessStatusCode();

        var page = await _factory.CreateClient()
            .GetFromJsonAsync<PagedResponse<ProductResponse>>($"/api/products?search={Uri.EscapeDataString(name.ToUpperInvariant())}");

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(name, page.Items[0].Name);
    }

    [Fact]
    public async Task UpdateProduct_ByAnotherUser_Returns403()
    {
        var (owner, _) = await _factory.CreateAuthenticatedClientAsync();
        var (stranger, _) = await _factory.CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(owner, quantity: 1);

        var response = await stranger.PutAsJsonAsync($"/api/products/{product.Id}",
            new UpdateProductRequest { Name = "Hijacked", Price = 1, Quantity = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_Invalid_Returns400WithFieldErrors()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest { Name = "", Price = -5, Quantity = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Name", problem!.Errors.Keys);
        Assert.Contains("Price", problem.Errors.Keys);
    }

    [Fact]
    public async Task PlaceOrder_DecrementsStock_AndAppearsInMyOrders()
    {
        var (seller, _) = await _factory.CreateAuthenticatedClientAsync();
        var (buyer, _) = await _factory.CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(seller, quantity: 5, price: 40m);

        var response = await buyer.PostAsJsonAsync("/api/orders", new CreateOrderRequest { ProductId = product.Id, Quantity = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
        Assert.Equal(80m, order.TotalAmount);

        var reloaded = await buyer.GetFromJsonAsync<ProductResponse>($"/api/products/{product.Id}");
        Assert.Equal(3, reloaded!.Quantity);

        var myOrders = await buyer.GetFromJsonAsync<List<OrderResponse>>("/api/orders/my");
        var mine = Assert.Single(myOrders!);
        Assert.Equal(product.Name, mine.Items.Single().ProductName);
    }

    [Fact]
    public async Task PlaceOrder_MoreThanStock_Returns400_AndKeepsStock()
    {
        var (seller, _) = await _factory.CreateAuthenticatedClientAsync();
        var (buyer, _) = await _factory.CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(seller, quantity: 1);

        var response = await buyer.PostAsJsonAsync("/api/orders", new CreateOrderRequest { ProductId = product.Id, Quantity = 2 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var reloaded = await buyer.GetFromJsonAsync<ProductResponse>($"/api/products/{product.Id}");
        Assert.Equal(1, reloaded!.Quantity);
    }

    [Fact]
    public async Task ConcurrentOrders_NeverOversell()
    {
        var (seller, _) = await _factory.CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(seller, quantity: 3);
        var buyers = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => _factory.CreateAuthenticatedClientAsync()));

        var responses = await Task.WhenAll(buyers.Select(b =>
            b.Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest { ProductId = product.Id, Quantity = 1 })));

        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest));
        var reloaded = await seller.GetFromJsonAsync<ProductResponse>($"/api/products/{product.Id}");
        Assert.Equal(0, reloaded!.Quantity);
    }

    private static CreateProductRequest NewProduct(string name, int quantity, decimal price = 10m)
        => new() { Name = name, Description = "Integration test product", Price = price, Quantity = quantity };

    private static async Task<ProductResponse> CreateProductAsync(HttpClient client, int quantity, decimal price = 10m)
    {
        var response = await client.PostAsJsonAsync("/api/products", NewProduct($"Product {Guid.NewGuid():N}", quantity, price));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }
}
