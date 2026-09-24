using InventoryPro.Api.Data;
using InventoryPro.Api.Services.Implementations;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Extensions;

/// <summary>
/// Groups application registrations so Program.cs stays short as the project grows.
/// In Spring Boot this is implicit component scanning; in .NET we write it by hand.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the EF Core DbContext.
    /// SQL Server is the default (and the provider used for migrations).
    /// SQLite is an optional local-development fallback for machines without SQL Server;
    /// enable it with the environment variable Database__Provider=Sqlite.
    /// </summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "SqlServer";
        var useSqlite = provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<AppDbContext>(options =>
        {
            if (useSqlite)
            {
                options.UseSqlite(configuration.GetConnectionString("SqliteConnection"));
            }
            else
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlServer => sqlServer.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            }
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Authentication / token services.
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // Catalog and administration services.
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUserService, UserService>();

        // Stock movement (purchases / sales), inventory ledger and reports.
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
