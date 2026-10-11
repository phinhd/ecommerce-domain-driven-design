using ECommerce.Domain.Shared.Enums;

namespace ECommerce.Domain.Entities;

public class Order : BaseClass
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public OrderStatusEnum Status { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    /// <summary>
    /// Places a pending order for a single product. Prices are captured at order time,
    /// so later price changes on the product do not alter the order.
    /// </summary>
    public static Order Place(Guid userId, Product product, int quantity, DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var item = OrderItem.For(product, quantity, nowUtc);
        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatusEnum.PENDING,
            CreatedAt = nowUtc,
            TotalAmount = item.TotalPrice,
            OrderItems = new List<OrderItem> { item }
        };
    }
}
