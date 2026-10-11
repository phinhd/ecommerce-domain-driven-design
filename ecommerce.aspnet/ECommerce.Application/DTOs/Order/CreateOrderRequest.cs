using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Order;

public sealed class CreateOrderRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000.")]
    public int Quantity { get; set; }
}
