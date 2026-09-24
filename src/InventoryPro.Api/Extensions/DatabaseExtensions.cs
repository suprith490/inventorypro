using InventoryPro.Api.Data;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Extensions;

/// <summary>
/// Creates/updates the schema and seeds baseline data at application startup.
/// In Spring Boot this is a Flyway migration plus a CommandLineRunner.
/// </summary>
public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var provider = scope.ServiceProvider;
        var context = provider.GetRequiredService<AppDbContext>();
        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var logger = provider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            var useSqlite = (configuration["Database:Provider"] ?? "SqlServer")
                .Equals("Sqlite", StringComparison.OrdinalIgnoreCase);

            if (useSqlite)
            {
                // SQLite dev fallback: build the schema from the model.
                // Migrations in this project are generated for SQL Server.
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("SQLite database ensured.");
            }
            else
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("SQL Server migrations applied.");
            }

            await DbSeeder.SeedAsync(context, passwordHasher, logger);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database initialization failed.");
            throw;
        }
    }
}
