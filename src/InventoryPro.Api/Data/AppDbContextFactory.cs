using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace InventoryPro.Api.Data;

/// <summary>
/// Used only by the "dotnet ef" command-line tool at design time
/// (dotnet ef migrations add / database update).
/// It lets EF build an AppDbContext without starting the whole web server.
/// Migrations are always generated for SQL Server, the production provider.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(GetBasePath())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sqlServer => sqlServer.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

        return new AppDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Tries the current directory first (when "dotnet ef" runs inside the project
    /// folder) and falls back to the compiled output folder.
    /// </summary>
    private static string GetBasePath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
        {
            return currentDirectory;
        }

        return Path.GetDirectoryName(typeof(AppDbContextFactory).Assembly.Location)
               ?? currentDirectory;
    }
}
