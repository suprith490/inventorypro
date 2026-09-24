using InventoryPro.Api.Data;
using InventoryPro.Api.DTOs.Reports;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Services.Implementations;

/// <summary>
/// Builds the aggregate reports. Everything is computed in SQL through EF Core
/// (GROUP BY / SUM / COUNT) and projected straight into DTOs, so we do not load
/// thousands of rows into memory.
/// </summary>
public class ReportService : IReportService
{
    private const int TopProductCount = 5;

    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryValuationReportDto> GetInventoryValuationAsync()
    {
        var categories = await _context.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .GroupBy(product => new { product.CategoryId, CategoryName = product.Category!.Name })
            .Select(group => new CategoryValuationDto
            {
                CategoryId = group.Key.CategoryId,
                CategoryName = group.Key.CategoryName,
                ProductCount = group.Count(),
                TotalUnits = group.Sum(product => product.QuantityInStock),
                CostValue = group.Sum(product => product.CostPrice * product.QuantityInStock),
                RetailValue = group.Sum(product => product.UnitPrice * product.QuantityInStock)
            })
            .OrderByDescending(item => item.CostValue)
            .ToListAsync();

        var totalCost = categories.Sum(item => item.CostValue);
        var totalRetail = categories.Sum(item => item.RetailValue);

        return new InventoryValuationReportDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ProductCount = categories.Sum(item => item.ProductCount),
            TotalUnits = categories.Sum(item => item.TotalUnits),
            TotalCostValue = totalCost,
            TotalRetailValue = totalRetail,
            PotentialProfit = totalRetail - totalCost,
            Categories = categories
        };
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime? from, DateTime? to)
    {
        var salesQuery = BuildSalesQuery(from, to);
        var itemsQuery = BuildSaleItemsQuery(from, to);

        var totalOrders = await salesQuery.CountAsync();
        var totalRevenue = await salesQuery
            .Select(sale => (decimal?)sale.TotalAmount)
            .SumAsync() ?? 0m;

        var totalUnitsSold = await itemsQuery
            .Select(item => (int?)item.Quantity)
            .SumAsync() ?? 0;

        // Cost of goods sold uses the product's current cost price.
        var totalCost = await itemsQuery
            .Select(item => (decimal?)(item.Quantity * item.Product!.CostPrice))
            .SumAsync() ?? 0m;

        var dailyTotals = await salesQuery
            .GroupBy(sale => sale.SaleDate.Date)
            .Select(group => new
            {
                Date = group.Key,
                OrderCount = group.Count(),
                Revenue = group.Sum(sale => sale.TotalAmount)
            })
            .ToListAsync();

        var dailyUnits = await itemsQuery
            .GroupBy(item => item.Sale!.SaleDate.Date)
            .Select(group => new { Date = group.Key, Units = group.Sum(item => item.Quantity) })
            .ToListAsync();

        var daily = dailyTotals
            .Select(day => new DailySalesDto
            {
                Date = day.Date,
                OrderCount = day.OrderCount,
                Revenue = day.Revenue,
                UnitsSold = dailyUnits.FirstOrDefault(unit => unit.Date == day.Date)?.Units ?? 0
            })
            .OrderBy(day => day.Date)
            .ToList();

        var topProducts = await itemsQuery
            .GroupBy(item => new { item.ProductId, SKU = item.Product!.SKU, Name = item.Product!.Name })
            .Select(group => new TopSellingProductDto
            {
                ProductId = group.Key.ProductId,
                SKU = group.Key.SKU,
                Name = group.Key.Name,
                UnitsSold = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.LineTotal)
            })
            .OrderByDescending(item => item.UnitsSold)
            .Take(TopProductCount)
            .ToListAsync();

        return new SalesReportDto
        {
            From = from,
            To = to,
            TotalOrders = totalOrders,
            TotalUnitsSold = totalUnitsSold,
            TotalRevenue = totalRevenue,
            TotalCostOfGoods = totalCost,
            GrossProfit = totalRevenue - totalCost,
            DailyBreakdown = daily,
            TopProducts = topProducts
        };
    }

    public async Task<PurchaseReportDto> GetPurchaseReportAsync(DateTime? from, DateTime? to)
    {
        var purchasesQuery = BuildPurchasesQuery(from, to);
        var itemsQuery = BuildPurchaseItemsQuery(from, to);

        var totalOrders = await purchasesQuery.CountAsync();
        var totalSpend = await purchasesQuery
            .Select(purchase => (decimal?)purchase.TotalAmount)
            .SumAsync() ?? 0m;

        var totalUnitsReceived = await itemsQuery
            .Select(item => (int?)item.Quantity)
            .SumAsync() ?? 0;

        var orderTotals = await purchasesQuery
            .GroupBy(purchase => new { purchase.SupplierId, SupplierName = purchase.Supplier!.Name })
            .Select(group => new
            {
                group.Key.SupplierId,
                group.Key.SupplierName,
                OrderCount = group.Count(),
                TotalSpend = group.Sum(purchase => purchase.TotalAmount)
            })
            .ToListAsync();

        var unitTotals = await itemsQuery
            .GroupBy(item => item.Purchase!.SupplierId)
            .Select(group => new { SupplierId = group.Key, Units = group.Sum(item => item.Quantity) })
            .ToListAsync();

        var bySupplier = orderTotals
            .Select(supplier => new SupplierPurchaseDto
            {
                SupplierId = supplier.SupplierId,
                SupplierName = supplier.SupplierName,
                OrderCount = supplier.OrderCount,
                TotalSpend = supplier.TotalSpend,
                UnitsReceived = unitTotals.FirstOrDefault(unit => unit.SupplierId == supplier.SupplierId)?.Units ?? 0
            })
            .OrderByDescending(item => item.TotalSpend)
            .ToList();

        return new PurchaseReportDto
        {
            From = from,
            To = to,
            TotalOrders = totalOrders,
            TotalUnitsReceived = totalUnitsReceived,
            TotalSpend = totalSpend,
            BySupplier = bySupplier
        };
    }

    private IQueryable<Sale> BuildSalesQuery(DateTime? from, DateTime? to)
    {
        var query = _context.Sales
            .AsNoTracking()
            .Where(sale => sale.Status == SaleStatus.Completed);

        if (from.HasValue)
        {
            query = query.Where(sale => sale.SaleDate >= from.Value);
        }

        if (to.HasValue)
        {
            var exclusiveEnd = to.Value.Date.AddDays(1);
            query = query.Where(sale => sale.SaleDate < exclusiveEnd);
        }

        return query;
    }

    private IQueryable<SaleItem> BuildSaleItemsQuery(DateTime? from, DateTime? to)
    {
        var query = _context.SaleItems
            .AsNoTracking()
            .Where(item => item.Sale!.Status == SaleStatus.Completed);

        if (from.HasValue)
        {
            query = query.Where(item => item.Sale!.SaleDate >= from.Value);
        }

        if (to.HasValue)
        {
            var exclusiveEnd = to.Value.Date.AddDays(1);
            query = query.Where(item => item.Sale!.SaleDate < exclusiveEnd);
        }

        return query;
    }

    private IQueryable<Purchase> BuildPurchasesQuery(DateTime? from, DateTime? to)
    {
        var query = _context.Purchases.AsNoTracking().AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(purchase => purchase.PurchaseDate >= from.Value);
        }

        if (to.HasValue)
        {
            var exclusiveEnd = to.Value.Date.AddDays(1);
            query = query.Where(purchase => purchase.PurchaseDate < exclusiveEnd);
        }

        return query;
    }

    private IQueryable<PurchaseItem> BuildPurchaseItemsQuery(DateTime? from, DateTime? to)
    {
        var query = _context.PurchaseItems.AsNoTracking().AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(item => item.Purchase!.PurchaseDate >= from.Value);
        }

        if (to.HasValue)
        {
            var exclusiveEnd = to.Value.Date.AddDays(1);
            query = query.Where(item => item.Purchase!.PurchaseDate < exclusiveEnd);
        }

        return query;
    }
}
