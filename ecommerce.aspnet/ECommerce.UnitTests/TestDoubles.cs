using ECommerce.Application.Common;
using ECommerce.Domain.Interfaces;
using Moq;

namespace ECommerce.UnitTests;

internal sealed class FakeClock : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
}

/// <summary>Unit of work mock with every repository wired up, so each test only sets up what it needs.</summary>
internal sealed class UnitOfWorkMock
{
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();
    public Mock<IUserRepository> Users { get; } = new();
    public Mock<IProductRepository> Products { get; } = new();
    public Mock<IOrderRepository> Orders { get; } = new();
    public Mock<IRefreshTokenRepository> RefreshTokens { get; } = new();
    public Mock<IUnitOfWorkTransaction> Transaction { get; } = new();

    public UnitOfWorkMock()
    {
        UnitOfWork.Setup(u => u.Users).Returns(Users.Object);
        UnitOfWork.Setup(u => u.Products).Returns(Products.Object);
        UnitOfWork.Setup(u => u.Orders).Returns(Orders.Object);
        UnitOfWork.Setup(u => u.RefreshTokens).Returns(RefreshTokens.Object);
        UnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Transaction.Object);
    }

    public IUnitOfWork Object => UnitOfWork.Object;
}
