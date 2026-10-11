using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure.Repositories;

/// <summary>
/// One DbContext per request; the DI container owns its lifetime, so the unit of work does not dispose it.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ECommerceDbContext _context;
    private IUserRepository? _userRepository;
    private IProductRepository? _productRepository;
    private IOrderRepository? _orderRepository;
    private IRefreshTokenRepository? _refreshTokenRepository;

    public UnitOfWork(ECommerceDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _userRepository ??= new UserRepository(_context);

    public IProductRepository Products => _productRepository ??= new ProductRepository(_context);

    public IOrderRepository Orders => _orderRepository ??= new OrderRepository(_context);

    public IRefreshTokenRepository RefreshTokens => _refreshTokenRepository ??= new RefreshTokenRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => new EfTransaction(await _context.Database.BeginTransactionAsync(cancellationToken));

    private sealed class EfTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfTransaction(IDbContextTransaction transaction) => _transaction = transaction;

        public Task CommitAsync(CancellationToken cancellationToken = default) => _transaction.CommitAsync(cancellationToken);

        // Disposing an uncommitted transaction rolls it back.
        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
