using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Order;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Services;

public sealed class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public OrderService(IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new BadRequestException("Quantity must be greater than zero.");

        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || product.IsDeleted())
            throw new NotFoundException("Product not found.");

        if (product.IsOwnedBy(userId))
            throw new BadRequestException("You cannot order your own product.");

        // Stock is decremented with a conditional UPDATE inside the same transaction as the
        // order insert: either both happen or neither does, and concurrent orders cannot oversell.
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        if (!await _unitOfWork.Products.TryDecreaseStockAsync(product.Id, request.Quantity, cancellationToken))
            throw new BadRequestException($"Not enough stock. Available: {product.Quantity}");

        var order = Order.Place(userId, product, request.Quantity, _clock.UtcNow);
        await _unitOfWork.Orders.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return MapToResponse(order);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Orders.GetByUserIdAsync(userId, cancellationToken);
        return orders.Select(MapToResponse).ToList();
    }

    private static OrderResponse MapToResponse(Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        Status = order.Status.ToString(),
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt ?? DateTime.MinValue,
        Items = order.OrderItems.Select(item => new OrderItemResponse
        {
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? "Unknown product",
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice
        }).ToList()
    };
}
