using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.DTOs.Products;
using InventoryPro.Api.Services.Implementations;
using Xunit;

namespace InventoryPro.Api.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesSku_AndStartsWithZeroStock()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new ProductService(db.Context);

        var product = await service.CreateAsync(new CreateProductRequestDto
        {
            SKU = "  abc-123 ",
            Name = "Bluetooth Speaker",
            CategoryId = seed.Category.Id,
            SupplierId = seed.Supplier.Id,
            UnitPrice = 39.99m,
            CostPrice = 20.00m,
            ReorderLevel = 5
        });

        Assert.Equal("ABC-123", product.SKU);
        // Stock always starts at zero and enters through purchases/adjustments only.
        Assert.Equal(0, product.QuantityInStock);
        Assert.Equal("Electronics", product.CategoryName);
        Assert.Equal("Test Supplier", product.SupplierName);
    }

    [Fact]
    public async Task CreateAsync_ThrowsConflict_WhenSkuAlreadyExists()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new ProductService(db.Context);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CreateProductRequestDto
        {
            SKU = "test-001",
            Name = "Duplicate",
            CategoryId = seed.Category.Id,
            UnitPrice = 1m,
            CostPrice = 1m,
            ReorderLevel = 1
        }));
    }

    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenCategoryDoesNotExist()
    {
        using var db = new TestDatabase();
        await db.SeedBaselineAsync();
        var service = new ProductService(db.Context);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreateProductRequestDto
        {
            SKU = "NEW-001",
            Name = "Orphan product",
            CategoryId = 9999,
            UnitPrice = 1m,
            CostPrice = 1m,
            ReorderLevel = 1
        }));
    }

    [Fact]
    public async Task GetPagedAsync_Search_MatchesNameAndSku()
    {
        using var db = new TestDatabase();
        await db.SeedBaselineAsync();
        var service = new ProductService(db.Context);

        var byName = await service.GetPagedAsync(new ProductQueryParameters { Search = "mouse" });
        var bySku = await service.GetPagedAsync(new ProductQueryParameters { Search = "TEST-002" });
        var noMatch = await service.GetPagedAsync(new ProductQueryParameters { Search = "does-not-exist" });

        Assert.Single(byName.Items);
        Assert.Equal("Wireless Mouse", byName.Items[0].Name);
        Assert.Single(bySku.Items);
        Assert.Equal("USB-C Hub", bySku.Items[0].Name);
        Assert.Empty(noMatch.Items);
    }

    [Fact]
    public async Task GetPagedAsync_LowStockOnly_ReturnsProductsAtOrBelowReorderLevel()
    {
        using var db = new TestDatabase();
        await db.SeedBaselineAsync();
        var service = new ProductService(db.Context);

        var result = await service.GetPagedAsync(new ProductQueryParameters { LowStockOnly = true });

        Assert.Single(result.Items);
        Assert.Equal("USB-C Hub", result.Items[0].Name);
        Assert.True(result.Items[0].IsLowStock);
    }

    [Fact]
    public async Task GetPagedAsync_SortsByPriceDescending()
    {
        using var db = new TestDatabase();
        await db.SeedBaselineAsync();
        var service = new ProductService(db.Context);

        var result = await service.GetPagedAsync(new ProductQueryParameters
        {
            SortBy = "price",
            SortDescending = true
        });

        Assert.Equal(49.50m, result.Items[0].UnitPrice);
        Assert.Equal(25.00m, result.Items[1].UnitPrice);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_AndHidesFromDefaultListing()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new ProductService(db.Context);

        await service.DeleteAsync(seed.RichStockProduct.Id);

        var defaultList = await service.GetPagedAsync(new ProductQueryParameters());
        var includingInactive = await service.GetPagedAsync(new ProductQueryParameters { IncludeInactive = true });

        Assert.DoesNotContain(defaultList.Items, product => product.Id == seed.RichStockProduct.Id);
        Assert.Contains(includingInactive.Items, product => product.Id == seed.RichStockProduct.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFound_WhenMissing()
    {
        using var db = new TestDatabase();
        await db.SeedBaselineAsync();
        var service = new ProductService(db.Context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(4242));
    }
}
