using ECommerce.Application.DTOs.Order;

namespace ECommerce.Application.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(Guid userId, CancellationToken cancellationToken = default);
}
