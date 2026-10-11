using ECommerce.Application.DTOs.Order;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Shared.Enums;
using Moq;

namespace ECommerce.UnitTests;

public sealed class OrderServiceTests
{
    private readonly UnitOfWorkMock _uow = new();
    private readonly FakeClock _clock = new();
    private readonly OrderService _sut;
    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Product _product;

    public OrderServiceTests()
    {
        _product = Product.Create(Guid.NewGuid(), "Laptop", null, 250m, 5, null, _clock.UtcNow);
        _uow.Products.Setup(r => r.GetByIdAsync(_product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_product);
        _sut = new OrderService(_uow.Object, _clock);
    }

    [Fact]
    public async Task CreateOrder_ReservesStock_PersistsOrder_AndCommits()
    {
        _uow.Products.Setup(r => r.TryDecreaseStockAsync(_product.Id, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Order? saved = null;
        _uow.Orders.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => saved = o);

        var result = await _sut.CreateOrderAsync(_buyerId, new CreateOrderRequest { ProductId = _product.Id, Quantity = 2 });

        Assert.NotNull(saved);
        Assert.Equal(OrderStatusEnum.PENDING, saved.Status);
        Assert.Equal(500m, saved.TotalAmount);
        var item = Assert.Single(saved.OrderItems);
        Assert.Equal(250m, item.UnitPrice);
        Assert.Equal(500m, item.TotalPrice);
        Assert.Equal(500m, result.TotalAmount);
        _uow.Transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WhenStockIsInsufficient_ThrowsAndDoesNotCommit()
    {
        _uow.Products.Setup(r => r.TryDecreaseStockAsync(_product.Id, 10, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _sut.CreateOrderAsync(_buyerId, new CreateOrderRequest { ProductId = _product.Id, Quantity = 10 }));

        _uow.Orders.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _uow.Transaction.Verify(t => t.DisposeAsync(), Times.Once); // disposing uncommitted = rollback
    }

    [Fact]
    public async Task CreateOrder_ForOwnProduct_Throws()
    {
        await Assert.ThrowsAsync<BadRequestException>(
            () => _sut.CreateOrderAsync(_product.CreatedByUserId, new CreateOrderRequest { ProductId = _product.Id, Quantity = 1 }));
    }

    [Fact]
    public async Task CreateOrder_ForDeletedProduct_ThrowsNotFound()
    {
        _product.SoftDelete(_clock.UtcNow);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CreateOrderAsync(_buyerId, new CreateOrderRequest { ProductId = _product.Id, Quantity = 1 }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CreateOrder_WithNonPositiveQuantity_Throws(int quantity)
    {
        await Assert.ThrowsAsync<BadRequestException>(
            () => _sut.CreateOrderAsync(_buyerId, new CreateOrderRequest { ProductId = _product.Id, Quantity = quantity }));
    }
}
