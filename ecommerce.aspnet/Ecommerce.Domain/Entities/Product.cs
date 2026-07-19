namespace ECommerce.Domain.Entities;

public class Product : BaseClass
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
