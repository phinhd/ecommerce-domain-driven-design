using ECommerce.Application.DTOs.Order;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Shared.Enums;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> CreateOrderAsync(Guid userId, Guid productId, int quantity)
    {
        if (quantity <= 0)
            throw new BadRequestException("Quantity must be greater than zero.");

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
            throw new NotFoundException("Product not found.");

        if (product.CreatedByUserId == userId)
            throw new BadRequestException("You cannot order your own product.");

        if (product.Quantity < quantity)
            throw new BadRequestException($"Not enough stock. Available: {product.Quantity}");

        product.Quantity -= quantity;
        _unitOfWork.Products.Update(product);

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatusEnum.PENDING,
            TotalAmount = quantity * product.Price,
            OrderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                }
            }
        };

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(order);
    }

    public async Task<IEnumerable<OrderResponse>> GetMyOrdersAsync(Guid userId)
    {
        var orders = await _unitOfWork.Orders.GetByUserIdAsync(userId);
        return orders.Select(MapToResponse);
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt ?? DateTime.UtcNow,
            Items = order.OrderItems.Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? "Unknown Product",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice
            }).ToList()
        };
    }
}
