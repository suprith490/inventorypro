using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Data;

/// <summary>
/// Inserts baseline data the first time the database is created.
/// In Spring Boot you would do this with data.sql, Flyway callbacks,
/// or a CommandLineRunner bean.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        ILogger logger)
    {
        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("Database already contains data. Skipping seed.");
            return;
        }

        var users = new List<User>
        {
            new()
            {
                FullName = "System Administrator",
                Email = "admin@inventorypro.local",
                PasswordHash = passwordHasher.Hash("Admin@123"),
                Role = UserRole.Admin,
                IsActive = true
            },
            new()
            {
                FullName = "Store Staff",
                Email = "staff@inventorypro.local",
                PasswordHash = passwordHasher.Hash("Staff@123"),
                Role = UserRole.Staff,
                IsActive = true
            }
        };

        var categories = new List<Category>
        {
            new() { Name = "Electronics", Description = "Computer and electronic accessories" },
            new() { Name = "Stationery", Description = "Office and writing supplies" },
            new() { Name = "Furniture", Description = "Office furniture" },
            new() { Name = "Groceries", Description = "Pantry and break-room supplies" }
        };

        var suppliers = new List<Supplier>
        {
            new()
            {
                Name = "TechSource Ltd",
                Email = "sales@techsource.local",
                Phone = "+1-555-0100",
                Address = "12 Innovation Drive, Austin, TX"
            },
            new()
            {
                Name = "OfficeMart Supplies",
                Email = "orders@officemart.local",
                Phone = "+1-555-0110",
                Address = "88 Commerce Street, Chicago, IL"
            },
            new()
            {
                Name = "Global Furnishings",
                Email = "hello@globalfurnishings.local",
                Phone = "+1-555-0120",
                Address = "5 Warehouse Road, Denver, CO"
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.Categories.AddRangeAsync(categories);
        await context.Suppliers.AddRangeAsync(suppliers);

        // Save first so products can reference the generated ids.
        await context.SaveChangesAsync();

        var electronics = categories.Single(category => category.Name == "Electronics");
        var stationery = categories.Single(category => category.Name == "Stationery");
        var furniture = categories.Single(category => category.Name == "Furniture");
        var groceries = categories.Single(category => category.Name == "Groceries");

        var techSource = suppliers.Single(supplier => supplier.Name == "TechSource Ltd");
        var officeMart = suppliers.Single(supplier => supplier.Name == "OfficeMart Supplies");
        var globalFurnishings = suppliers.Single(supplier => supplier.Name == "Global Furnishings");

        var products = new List<Product>
        {
            new()
            {
                SKU = "ELEC-001", Name = "Wireless Mouse",
                Description = "2.4GHz ergonomic wireless mouse",
                Category = electronics, Supplier = techSource,
                UnitPrice = 29.99m, CostPrice = 15.00m,
                QuantityInStock = 120, ReorderLevel = 20
            },
            new()
            {
                SKU = "ELEC-002", Name = "Mechanical Keyboard",
                Description = "RGB backlit mechanical keyboard",
                Category = electronics, Supplier = techSource,
                UnitPrice = 89.99m, CostPrice = 55.00m,
                QuantityInStock = 45, ReorderLevel = 10
            },
            new()
            {
                SKU = "ELEC-003", Name = "27-inch Monitor",
                Description = "1080p IPS monitor",
                Category = electronics, Supplier = techSource,
                UnitPrice = 229.00m, CostPrice = 180.00m,
                QuantityInStock = 8, ReorderLevel = 10
            },
            new()
            {
                SKU = "ELEC-004", Name = "USB-C Hub",
                Description = "7-in-1 USB-C docking hub",
                Category = electronics, Supplier = techSource,
                UnitPrice = 49.50m, CostPrice = 28.00m,
                QuantityInStock = 5, ReorderLevel = 15
            },
            new()
            {
                SKU = "STAT-001", Name = "A4 Notebook",
                Description = "200-page ruled notebook",
                Category = stationery, Supplier = officeMart,
                UnitPrice = 4.99m, CostPrice = 2.10m,
                QuantityInStock = 300, ReorderLevel = 50
            },
            new()
            {
                SKU = "STAT-002", Name = "Gel Pen Pack",
                Description = "Pack of 10 blue gel pens",
                Category = stationery, Supplier = officeMart,
                UnitPrice = 6.50m, CostPrice = 3.00m,
                QuantityInStock = 12, ReorderLevel = 25
            },
            new()
            {
                SKU = "FURN-001", Name = "Ergonomic Office Chair",
                Description = "Adjustable mesh office chair",
                Category = furniture, Supplier = globalFurnishings,
                UnitPrice = 189.00m, CostPrice = 120.00m,
                QuantityInStock = 22, ReorderLevel = 5
            },
            new()
            {
                SKU = "GROC-001", Name = "Coffee Beans 1kg",
                Description = "Medium roast whole bean coffee",
                Category = groceries, Supplier = officeMart,
                UnitPrice = 24.00m, CostPrice = 14.00m,
                QuantityInStock = 60, ReorderLevel = 20
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seeded {Users} users, {Categories} categories, {Suppliers} suppliers, {Products} products.",
            users.Count, categories.Count, suppliers.Count, products.Count);
    }
}
