using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        await SeedAdminUserAsync(serviceProvider);
        await SeedSampleUsersAsync(serviceProvider);
        await SeedSampleProductsAsync(serviceProvider);
    }

    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DataSeeder));

        var adminEmail = configuration["AdminSeedSettings:Email"];
        var adminPassword = configuration["AdminSeedSettings:Password"];

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            logger.LogWarning("AdminSeedSettings:Email or AdminSeedSettings:Password is not configured. Skipping admin user seed.");
            return;
        }

        var existingAdmin = await unitOfWork.Users.GetByEmailAsync(adminEmail);
        
        if (existingAdmin == null)
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                PasswordHash = passwordHasher.HashPassword(adminPassword),
                Username = "Admin",
                Type = UserTypeEnum.ADMIN,
                Status = UserStatusEnum.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await unitOfWork.Users.AddAsync(adminUser);
            await unitOfWork.SaveChangesAsync();
            
            logger.LogInformation("Admin user seeded successfully with email: {AdminEmail}", adminEmail);
        }
        else
        {
            logger.LogInformation("Admin user with email {AdminEmail} already exists. Skipping seed.", adminEmail);
        }
    }

    public static async Task SeedSampleUsersAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DataSeeder));

        var sampleUsers = new[]
        {
            new { Email = "customer1@example.com", Username = "CustomerOne", Password = "Password123!" },
            new { Email = "customer2@example.com", Username = "CustomerTwo", Password = "Password123!" }
        };

        foreach (var user in sampleUsers)
        {
            var existing = await unitOfWork.Users.GetByEmailAsync(user.Email);
            if (existing == null)
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = user.Email,
                    Username = user.Username,
                    PasswordHash = passwordHasher.HashPassword(user.Password),
                    Type = UserTypeEnum.CUSTOMER,
                    Status = UserStatusEnum.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await unitOfWork.Users.AddAsync(newUser);
                logger.LogInformation("Sample user {Email} seeded.", user.Email);
            }
        }
        await unitOfWork.SaveChangesAsync();
    }

    public static async Task SeedSampleProductsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DataSeeder));

        var existingProducts = await unitOfWork.Products.GetAllAsync();
        if (existingProducts.Any()) return;

        var users = await unitOfWork.Users.GetAllAsync();
        var customer1 = users.FirstOrDefault(u => u.Email == "customer1@example.com");
        var customer2 = users.FirstOrDefault(u => u.Email == "customer2@example.com");

        if (customer1 == null || customer2 == null) return;

        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "iPhone 15", Description = "Latest Apple Phone", Price = 999, Quantity = 10, ImageUrl = "https://picsum.photos/200", CreatedByUserId = customer1.Id },
            new() { Id = Guid.NewGuid(), Name = "iPhone 14", Description = "Reliable Apple Phone", Price = 799, Quantity = 15, ImageUrl = "https://picsum.photos/200", CreatedByUserId = customer1.Id },
            new() { Id = Guid.NewGuid(), Name = "Samsung Galaxy Phone", Description = "Android flagship", Price = 899, Quantity = 12, ImageUrl = "https://picsum.photos/200", CreatedByUserId = customer2.Id },
            new() { Id = Guid.NewGuid(), Name = "Gaming Phone X", Description = "High performance phone", Price = 699, Quantity = 8, ImageUrl = "https://picsum.photos/200", CreatedByUserId = customer2.Id },
            new() { Id = Guid.NewGuid(), Name = "Laptop Dell XPS", Description = "Premium Windows Laptop", Price = 1200, Quantity = 5, ImageUrl = "https://picsum.photos/200", CreatedByUserId = customer1.Id },
            new() { Id = Guid.NewGuid(), Name = "Laptop HP Spectre", Description = "Sleek HP Laptop", Price = 1100, Quantity = 7, ImageUrl = "https://picsum.photos/200", CreatedByUserId = customer2.Id },
            new() { Id = Guid.NewGuid(), Name = "Budget Laptop", Description = "Affordable laptop", Price = 400, Quantity = 20, ImageUrl = "https://picsum.photos/200", CreatedByUserId = customer1.Id }
        };

        foreach (var product in products)
        {
            await unitOfWork.Products.AddAsync(product);
        }
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("Sample products seeded.");
    }
}