using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent seeding. The admin account is created only when AdminSeedSettings is configured;
/// demo users and products only when "Seed:SampleData" is true (Development / Docker Compose).
/// </summary>
public static class DataSeeder
{
    public const string SamplePassword = "Password123!";

    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var configuration = services.GetRequiredService<IConfiguration>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DataSeeder));

        await SeedAdminUserAsync(configuration, unitOfWork, passwordHasher, logger, cancellationToken);

        if (bool.TryParse(configuration["Seed:SampleData"], out var seedSampleData) && seedSampleData)
            await SeedSampleDataAsync(unitOfWork, passwordHasher, logger, cancellationToken);
    }

    private static async Task SeedAdminUserAsync(
        IConfiguration configuration, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ILogger logger, CancellationToken cancellationToken)
    {
        var adminEmail = configuration["AdminSeedSettings:Email"];
        var adminPassword = configuration["AdminSeedSettings:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogInformation("AdminSeedSettings not configured; skipping admin seed.");
            return;
        }

        if (await unitOfWork.Users.GetByEmailAsync(adminEmail, cancellationToken) is not null)
            return;

        await unitOfWork.Users.AddAsync(NewUser(adminEmail, "Admin", adminPassword, UserTypeEnum.ADMIN, passwordHasher), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded admin user {AdminEmail}.", adminEmail);
    }

    private static async Task SeedSampleDataAsync(
        IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ILogger logger, CancellationToken cancellationToken)
    {
        var customer1 = await EnsureUserAsync("customer1@example.com", "CustomerOne", unitOfWork, passwordHasher, cancellationToken);
        var customer2 = await EnsureUserAsync("customer2@example.com", "CustomerTwo", unitOfWork, passwordHasher, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (await unitOfWork.Products.AnyAsync(_ => true, cancellationToken))
            return;

        var now = DateTime.UtcNow;
        Product[] products =
        [
            Product.Create(customer1.Id, "iPhone 15", "Latest Apple phone", 999, 10, "https://picsum.photos/seed/iphone15/400", now),
            Product.Create(customer1.Id, "iPhone 14", "Reliable Apple phone", 799, 15, "https://picsum.photos/seed/iphone14/400", now),
            Product.Create(customer2.Id, "Samsung Galaxy Phone", "Android flagship", 899, 12, "https://picsum.photos/seed/galaxy/400", now),
            Product.Create(customer2.Id, "Gaming Phone X", "High performance phone", 699, 8, "https://picsum.photos/seed/gaming/400", now),
            Product.Create(customer1.Id, "Laptop Dell XPS", "Premium Windows laptop", 1200, 5, "https://picsum.photos/seed/xps/400", now),
            Product.Create(customer2.Id, "Laptop HP Spectre", "Sleek HP laptop", 1100, 7, "https://picsum.photos/seed/spectre/400", now),
            Product.Create(customer1.Id, "Budget Laptop", "Affordable laptop", 400, 20, "https://picsum.photos/seed/budget/400", now)
        ];

        foreach (var product in products)
            await unitOfWork.Products.AddAsync(product, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} sample products.", products.Length);
    }

    private static async Task<User> EnsureUserAsync(
        string email, string username, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
            return existing;

        var user = NewUser(email, username, SamplePassword, UserTypeEnum.CUSTOMER, passwordHasher);
        await unitOfWork.Users.AddAsync(user, cancellationToken);
        return user;
    }

    private static User NewUser(string email, string username, string password, UserTypeEnum type, IPasswordHasher passwordHasher) => new()
    {
        Id = Guid.NewGuid(),
        Email = User.NormalizeEmail(email),
        Username = username,
        PasswordHash = passwordHasher.HashPassword(password),
        Type = type,
        Status = UserStatusEnum.ACTIVE,
        CreatedAt = DateTime.UtcNow,
        IsDeleted = false
    };
}
