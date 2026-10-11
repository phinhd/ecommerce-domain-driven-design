using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Extensions;

public static class StartupExtensions
{
    /// <summary>
    /// Applies EF Core migrations and seeds data when "Database:ApplyMigrationsOnStartup" is true.
    /// Convenient for local development and Docker Compose; production should run migrations from the pipeline.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
            return;

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        await DataSeeder.SeedAsync(app.Services);
    }
}
