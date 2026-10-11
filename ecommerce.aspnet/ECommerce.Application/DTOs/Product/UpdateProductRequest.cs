using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Product;

public sealed class UpdateProductRequest
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0")]
    public int Quantity { get; set; }

    [MaxLength(2048)]
    public string? ImageUrl { get; set; }
}
