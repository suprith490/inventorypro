using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.DTOs.Sales;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryPro.Api.Tests;

public class SaleServiceTests
{
    [Fact]
    public async Task CreateAsync_DecreasesStock_AndWritesLedger()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new SaleService(db.Context);

        var sale = await service.CreateAsync(new CreateSaleRequestDto
        {
            CustomerName = "Acme Corp",
            Items = new List<CreateSaleItemDto>
            {
                new() { ProductId = seed.RichStockProduct.Id, Quantity = 4 }
            }
        }, seed.User.Id);

        Assert.StartsWith("SO-", sale.SaleNumber);
        Assert.Equal(100m, sale.TotalAmount); // 4 x 25.00 product unit price

        var product = await db.Context.Products.SingleAsync(p => p.Id == seed.RichStockProduct.Id);
        Assert.Equal(96, product.QuantityInStock);

        var ledger = await db.Context.InventoryTransactions.SingleAsync(t =>
            t.ProductId == seed.RichStockProduct.Id);
        Assert.Equal(TransactionType.StockOut, ledger.TransactionType);
        Assert.Equal(ReferenceType.Sale, ledger.ReferenceType);
        Assert.Equal(100, ledger.QuantityBefore);
        Assert.Equal(96, ledger.QuantityAfter);
    }

    [Fact]
    public async Task CreateAsync_UsesPriceOverride_WhenProvided()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new SaleService(db.Context);

        var sale = await service.CreateAsync(new CreateSaleRequestDto
        {
            Items = new List<CreateSaleItemDto>
            {
                new() { ProductId = seed.RichStockProduct.Id, Quantity = 2, UnitPrice = 19.99m }
            }
        }, seed.User.Id);

        Assert.Equal(39.98m, sale.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_ThrowsConflict_AndLeavesStockUntouched_WhenInsufficient()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new SaleService(db.Context);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CreateSaleRequestDto
        {
            Items = new List<CreateSaleItemDto>
            {
                new() { ProductId = seed.LowStockProduct.Id, Quantity = 999 }
            }
        }, seed.User.Id));

        var product = await db.Context.Products.SingleAsync(p => p.Id == seed.LowStockProduct.Id);
        Assert.Equal(3, product.QuantityInStock);
        Assert.Empty(await db.Context.Sales.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_ForInactiveProduct()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        seed.RichStockProduct.IsActive = false;
        await db.Context.SaveChangesAsync();
        var service = new SaleService(db.Context);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreateSaleRequestDto
        {
            Items = new List<CreateSaleItemDto>
            {
                new() { ProductId = seed.RichStockProduct.Id, Quantity = 1 }
            }
        }, seed.User.Id));
    }
}
