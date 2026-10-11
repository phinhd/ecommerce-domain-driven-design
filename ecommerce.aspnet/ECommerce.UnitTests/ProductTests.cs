using ECommerce.Domain.Entities;

namespace ECommerce.UnitTests;

public sealed class ProductTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_RejectsNegativePrice()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(Guid.NewGuid(), "x", null, -1m, 1, null, Now));

    [Fact]
    public void Create_RejectsBlankName()
        => Assert.Throws<ArgumentException>(() => Product.Create(Guid.NewGuid(), "  ", null, 1m, 1, null, Now));

    [Fact]
    public void Order_CapturesPriceAtPlacement()
    {
        var product = Product.Create(Guid.NewGuid(), "Desk", null, 80m, 10, null, Now);

        var order = Order.Place(Guid.NewGuid(), product, 3, Now);
        product.UpdateDetails("Desk", null, 999m, 10, null, Now);

        Assert.Equal(240m, order.TotalAmount);
        Assert.Equal(80m, order.OrderItems.Single().UnitPrice);
    }
}
