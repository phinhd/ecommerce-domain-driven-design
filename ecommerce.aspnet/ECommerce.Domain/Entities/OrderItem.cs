namespace ECommerce.Domain.Entities;

public class OrderItem : BaseClass
{
    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public static OrderItem For(Product product, int quantity, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = product.Id,
        Product = product,
        Quantity = quantity,
        UnitPrice = product.Price,
        TotalPrice = product.Price * quantity,
        CreatedAt = nowUtc
    };
}
