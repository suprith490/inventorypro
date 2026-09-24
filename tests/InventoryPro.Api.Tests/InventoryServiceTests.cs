using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.DTOs.Inventory;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryPro.Api.Tests;

public class InventoryServiceTests
{
    [Fact]
    public async Task AdjustStockAsync_UpdatesQuantity_AndRecordsAdjustment()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new InventoryService(db.Context);

        var transaction = await service.AdjustStockAsync(new AdjustStockRequestDto
        {
            ProductId = seed.RichStockProduct.Id,
            NewQuantity = 90,
            Reason = "Stock count correction"
        }, seed.User.Id);

        Assert.Equal("Adjustment", transaction.TransactionType);
        Assert.Equal(10, transaction.Quantity); // magnitude of the -10 change
        Assert.Equal(100, transaction.QuantityBefore);
        Assert.Equal(90, transaction.QuantityAfter);

        var product = await db.Context.Products.SingleAsync(p => p.Id == seed.RichStockProduct.Id);
        Assert.Equal(90, product.QuantityInStock);
    }

    [Fact]
    public async Task AdjustStockAsync_ThrowsBadRequest_WhenQuantityIsUnchanged()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new InventoryService(db.Context);

        await Assert.ThrowsAsync<BadRequestException>(() => service.AdjustStockAsync(new AdjustStockRequestDto
        {
            ProductId = seed.RichStockProduct.Id,
            NewQuantity = 100,
            Reason = "No change"
        }, seed.User.Id));
    }

    [Fact]
    public async Task AdjustStockAsync_ThrowsNotFound_WhenProductMissing()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new InventoryService(db.Context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.AdjustStockAsync(new AdjustStockRequestDto
        {
            ProductId = 9999,
            NewQuantity = 5,
            Reason = "Missing product"
        }, seed.User.Id));
    }

    [Fact]
    public async Task GetLowStockAsync_ReturnsOnlyProductsAtOrBelowReorderLevel()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new InventoryService(db.Context);

        var lowStock = await service.GetLowStockAsync();

        var item = Assert.Single(lowStock);
        Assert.Equal(seed.LowStockProduct.SKU, item.SKU);
        Assert.Equal(3, item.QuantityInStock);
        Assert.Equal(10, item.ReorderLevel);
        Assert.Equal(17, item.SuggestedReorderQuantity); // 2 * 10 - 3
        Assert.False(item.IsOutOfStock);
    }

    [Fact]
    public async Task GetTransactionsAsync_FiltersByTransactionType()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();

        var purchaseService = new PurchaseService(db.Context);
        await purchaseService.CreateAsync(new DTOs.Purchases.CreatePurchaseRequestDto
        {
            SupplierId = seed.Supplier.Id,
            Items = new List<DTOs.Purchases.CreatePurchaseItemDto>
            {
                new() { ProductId = seed.RichStockProduct.Id, Quantity = 5, UnitCost = 10m }
            }
        }, seed.User.Id);

        var service = new InventoryService(db.Context);
        var stockIn = await service.GetTransactionsAsync(new InventoryQueryParameters
        {
            TransactionType = TransactionType.StockIn
        });
        var stockOut = await service.GetTransactionsAsync(new InventoryQueryParameters
        {
            TransactionType = TransactionType.StockOut
        });

        Assert.Single(stockIn.Items);
        Assert.Empty(stockOut.Items);
    }
}
