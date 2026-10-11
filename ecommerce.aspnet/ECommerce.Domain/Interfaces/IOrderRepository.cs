using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
