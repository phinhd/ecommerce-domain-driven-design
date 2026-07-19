using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Product;

public class CreateProductRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0")]
    public int Quantity { get; set; }

    public string? ImageUrl { get; set; }
}