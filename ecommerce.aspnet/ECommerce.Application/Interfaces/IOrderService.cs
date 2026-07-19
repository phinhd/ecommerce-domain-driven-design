using ECommerce.Application.DTOs.Order;

namespace ECommerce.Application.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(Guid userId, Guid productId, int quantity);
    Task<IEnumerable<OrderResponse>> GetMyOrdersAsync(Guid userId);
}
