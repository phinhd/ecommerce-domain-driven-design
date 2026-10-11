namespace ECommerce.Domain.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an explicit database transaction. Use it when a use case mixes
    /// set-based updates with tracked changes and both must succeed together.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
