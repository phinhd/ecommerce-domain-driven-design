using ECommerce.Application.DTOs.Product;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using Moq;

namespace ECommerce.UnitTests;

public sealed class ProductServiceTests
{
    private readonly UnitOfWorkMock _uow = new();
    private readonly FakeClock _clock = new();
    private readonly ProductService _sut;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Product _product;

    public ProductServiceTests()
    {
        _product = Product.Create(_ownerId, "Phone", "desc", 100m, 3, null, _clock.UtcNow);
        _uow.Products.Setup(r => r.GetByIdAsync(_product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_product);
        _sut = new ProductService(_uow.Object, _clock);
    }

    [Fact]
    public async Task Update_ByOwner_ChangesDetails()
    {
        _clock.UtcNow = _clock.UtcNow.AddHours(1);

        var result = await _sut.UpdateProductAsync(_product.Id, new UpdateProductRequest { Name = " Phone 2 ", Price = 120m, Quantity = 4 }, _ownerId);

        Assert.Equal("Phone 2", result.Name);
        Assert.Equal(120m, result.Price);
        Assert.Equal(_clock.UtcNow, result.UpdatedAt);
        _uow.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ByAnotherUser_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.UpdateProductAsync(_product.Id, new UpdateProductRequest { Name = "x", Price = 1, Quantity = 1 }, Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_ByOwner_SoftDeletes()
    {
        await _sut.DeleteProductAsync(_product.Id, _ownerId);

        Assert.True(_product.IsDeleted());
    }

    [Fact]
    public async Task Delete_ByAnotherUser_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.DeleteProductAsync(_product.Id, Guid.NewGuid()));
        Assert.False(_product.IsDeleted());
    }

    [Fact]
    public async Task GetById_DeletedProduct_ThrowsNotFound()
    {
        _product.SoftDelete(_clock.UtcNow);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetProductByIdAsync(_product.Id));
    }

    [Fact]
    public async Task GetProducts_MineWithoutUser_IsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _sut.GetProductsAsync(new ProductQuery { Mine = true }, currentUserId: null));
    }

    [Fact]
    public async Task GetProducts_ClampsPageSize()
    {
        _uow.Products.Setup(r => r.SearchAsync(null, 1, 100, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product>(), 0));

        var result = await _sut.GetProductsAsync(new ProductQuery { PageSize = 5000 }, null);

        Assert.Equal(100, result.PageSize);
    }
}
