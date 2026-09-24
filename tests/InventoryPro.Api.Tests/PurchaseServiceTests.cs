using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.DTOs.Purchases;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryPro.Api.Tests;

public class PurchaseServiceTests
{
    [Fact]
    public async Task CreateAsync_IncreasesStock_WritesLedger_AndComputesTotal()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new PurchaseService(db.Context);

        var purchase = await service.CreateAsync(new CreatePurchaseRequestDto
        {
            SupplierId = seed.Supplier.Id,
            Notes = "Restock",
            Items = new List<CreatePurchaseItemDto>
            {
                new() { ProductId = seed.LowStockProduct.Id, Quantity = 50, UnitCost = 28.00m }
            }
        }, seed.User.Id);

        Assert.StartsWith("PO-", purchase.PurchaseNumber);
        Assert.Equal(1400m, purchase.TotalAmount);
        Assert.Equal(50, purchase.TotalQuantity);

        var product = await db.Context.Products.SingleAsync(p => p.Id == seed.LowStockProduct.Id);
        Assert.Equal(53, product.QuantityInStock); // 3 before + 50 received

        var ledger = await db.Context.InventoryTransactions.SingleAsync(t =>
            t.ProductId == seed.LowStockProduct.Id);
        Assert.Equal(TransactionType.StockIn, ledger.TransactionType);
        Assert.Equal(ReferenceType.Purchase, ledger.ReferenceType);
        Assert.Equal(3, ledger.QuantityBefore);
        Assert.Equal(53, ledger.QuantityAfter);
        Assert.Equal(purchase.Id, ledger.ReferenceId);
    }

    [Fact]
    public async Task CreateAsync_SupportsMultipleLines_AndAggregatesTotal()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new PurchaseService(db.Context);

        var purchase = await service.CreateAsync(new CreatePurchaseRequestDto
        {
            SupplierId = seed.Supplier.Id,
            Items = new List<CreatePurchaseItemDto>
            {
                new() { ProductId = seed.RichStockProduct.Id, Quantity = 10, UnitCost = 12.00m },
                new() { ProductId = seed.LowStockProduct.Id, Quantity = 5, UnitCost = 28.00m }
            }
        }, seed.User.Id);

        Assert.Equal(2, purchase.Items.Count);
        Assert.Equal(260m, purchase.TotalAmount); // 120 + 140
    }

    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenSupplierMissing()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new PurchaseService(db.Context);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreatePurchaseRequestDto
        {
            SupplierId = 9999,
            Items = new List<CreatePurchaseItemDto>
            {
                new() { ProductId = seed.RichStockProduct.Id, Quantity = 1, UnitCost = 1m }
            }
        }, seed.User.Id));
    }

    [Fact]
    public async Task CreateAsync_DoesNotChangeStock_WhenAProductIsMissing()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new PurchaseService(db.Context);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreatePurchaseRequestDto
        {
            SupplierId = seed.Supplier.Id,
            Items = new List<CreatePurchaseItemDto>
            {
                new() { ProductId = seed.RichStockProduct.Id, Quantity = 5, UnitCost = 10m },
                new() { ProductId = 9999, Quantity = 5, UnitCost = 10m }
            }
        }, seed.User.Id));

        // The whole document is rejected before any stock changes are applied.
        var product = await db.Context.Products.SingleAsync(p => p.Id == seed.RichStockProduct.Id);
        Assert.Equal(100, product.QuantityInStock);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsNewestFirst()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new PurchaseService(db.Context);

        await service.CreateAsync(new CreatePurchaseRequestDto
        {
            SupplierId = seed.Supplier.Id,
            PurchaseDate = new DateTime(2026, 1, 1),
            Items = new List<CreatePurchaseItemDto> { new() { ProductId = seed.RichStockProduct.Id, Quantity = 1, UnitCost = 1m } }
        }, seed.User.Id);

        await service.CreateAsync(new CreatePurchaseRequestDto
        {
            SupplierId = seed.Supplier.Id,
            PurchaseDate = new DateTime(2026, 6, 1),
            Items = new List<CreatePurchaseItemDto> { new() { ProductId = seed.RichStockProduct.Id, Quantity = 1, UnitCost = 1m } }
        }, seed.User.Id);

        var result = await service.GetPagedAsync(new PurchaseQueryParameters());

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new DateTime(2026, 6, 1), result.Items[0].PurchaseDate);
    }
}
