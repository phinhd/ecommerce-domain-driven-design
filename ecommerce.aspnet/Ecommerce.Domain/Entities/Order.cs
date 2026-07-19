using ECommerce.Domain.Shared.Enums;

namespace ECommerce.Domain.Entities;

public class Order : BaseClass
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public OrderStatusEnum Status { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}