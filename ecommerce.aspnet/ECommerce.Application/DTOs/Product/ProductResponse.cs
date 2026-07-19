namespace ECommerce.Application.DTOs.Product;

public class ProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
}
