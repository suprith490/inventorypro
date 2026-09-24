using InventoryPro.Api.Data;
using InventoryPro.Api.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Tests;

/// <summary>
/// Spins up an isolated SQLite in-memory database for one test.
///
/// Why SQLite in-memory instead of the EF Core InMemory provider?
/// The services use real database transactions (BeginTransactionAsync), which the
/// InMemory provider does not support. SQLite in-memory behaves like a real
/// relational database (constraints, transactions) while living entirely in RAM.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Context { get; }

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Context = CreateContext();
        Context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Seeds one category, one supplier, one admin user and a small product catalog.
    /// Returns the tracked entities so tests can reference their generated ids.
    /// </summary>
    public async Task<SeedResult> SeedBaselineAsync()
    {
        var user = new User
        {
            FullName = "Test Admin",
            Email = "admin@test.local",
            PasswordHash = "not-used-in-these-tests",
            Role = UserRole.Admin,
            IsActive = true
        };

        var category = new Category { Name = "Electronics", Description = "Test category", IsActive = true };
        var supplier = new Supplier { Name = "Test Supplier", IsActive = true };

        Context.Users.Add(user);
        Context.Categories.Add(category);
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync();

        var richStockProduct = new Product
        {
            SKU = "TEST-001",
            Name = "Wireless Mouse",
            CategoryId = category.Id,
            SupplierId = supplier.Id,
            UnitPrice = 25.00m,
            CostPrice = 12.00m,
            QuantityInStock = 100,
            ReorderLevel = 20,
            IsActive = true
        };

        var lowStockProduct = new Product
        {
            SKU = "TEST-002",
            Name = "USB-C Hub",
            CategoryId = category.Id,
            SupplierId = supplier.Id,
            UnitPrice = 49.50m,
            CostPrice = 28.00m,
            QuantityInStock = 3,
            ReorderLevel = 10,
            IsActive = true
        };

        Context.Products.AddRange(richStockProduct, lowStockProduct);
        await Context.SaveChangesAsync();

        return new SeedResult(user, category, supplier, richStockProduct, lowStockProduct);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

public sealed record SeedResult(
    User User,
    Category Category,
    Supplier Supplier,
    Product RichStockProduct,
    Product LowStockProduct);
