using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Persistence;

namespace ECommerce.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
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

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}