using InventoryPro.Api.Data;
using InventoryPro.Api.DTOs.Dashboard;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Services.Implementations;

/// <summary>
/// Builds the dashboard summaries with a handful of aggregate queries.
/// In Spring Boot you would write the same SQL through a repository and
/// map the projections into DTOs; EF Core LINQ translates to SQL the same way.
/// </summary>
public class DashboardService : IDashboardService
{
    private const int RecentActivityCount = 5;
    private const int LowStockPreviewCount = 5;

    private readonly AppDbContext _context;
    private readonly IInventoryService _inventoryService;

    public DashboardService(AppDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeProducts = _context.Products.AsNoTracking().Where(product => product.IsActive);

        var todaySales = _context.Sales.AsNoTracking()
            .Where(sale => sale.SaleDate >= today && sale.SaleDate < tomorrow);
        var todayPurchases = _context.Purchases.AsNoTracking()
            .Where(purchase => purchase.PurchaseDate >= today && purchase.PurchaseDate < tomorrow);

        var lowStock = await _inventoryService.GetLowStockAsync();

        return new AdminDashboardDto
        {
            ActiveProducts = await activeProducts.CountAsync(),
            TotalProducts = await _context.Products.AsNoTracking().CountAsync(),
            TotalUnits = await activeProducts.Select(product => (int?)product.QuantityInStock).SumAsync() ?? 0,
            TotalStockCostValue = await activeProducts
                .Select(product => (decimal?)(product.CostPrice * product.QuantityInStock)).SumAsync() ?? 0m,
            TotalStockRetailValue = await activeProducts
                .Select(product => (decimal?)(product.UnitPrice * product.QuantityInStock)).SumAsync() ?? 0m,
            LowStockCount = await activeProducts.CountAsync(product => product.QuantityInStock <= product.ReorderLevel),
            OutOfStockCount = await activeProducts.CountAsync(product => product.QuantityInStock <= 0),
            TotalCategories = await _context.Categories.AsNoTracking().CountAsync(category => category.IsActive),
            TotalSuppliers = await _context.Suppliers.AsNoTracking().CountAsync(supplier => supplier.IsActive),
            TotalUsers = await _context.Users.AsNoTracking().CountAsync(),
            TodaySalesCount = await todaySales.CountAsync(),
            TodaySalesAmount = await todaySales.Select(sale => (decimal?)sale.TotalAmount).SumAsync() ?? 0m,
            TodayPurchaseCount = await todayPurchases.CountAsync(),
            TodayPurchaseAmount = await todayPurchases.Select(purchase => (decimal?)purchase.TotalAmount).SumAsync() ?? 0m,
            MonthSalesAmount = await _context.Sales.AsNoTracking()
                .Where(sale => sale.SaleDate >= monthStart)
                .Select(sale => (decimal?)sale.TotalAmount).SumAsync() ?? 0m,
            MonthPurchaseAmount = await _context.Purchases.AsNoTracking()
                .Where(purchase => purchase.PurchaseDate >= monthStart)
                .Select(purchase => (decimal?)purchase.TotalAmount).SumAsync() ?? 0m,
            RecentSales = await GetRecentSalesAsync(),
            RecentPurchases = await GetRecentPurchasesAsync(),
            LowStockProducts = lowStock.Take(LowStockPreviewCount).ToList()
        };
    }

    public async Task<StaffDashboardDto> GetStaffDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var activeProducts = _context.Products.AsNoTracking().Where(product => product.IsActive);

        var todaySales = _context.Sales.AsNoTracking()
            .Where(sale => sale.SaleDate >= today && sale.SaleDate < tomorrow);
        var todayPurchases = _context.Purchases.AsNoTracking()
            .Where(purchase => purchase.PurchaseDate >= today && purchase.PurchaseDate < tomorrow);

        var lowStock = await _inventoryService.GetLowStockAsync();

        return new StaffDashboardDto
        {
            TotalProducts = await activeProducts.CountAsync(),
            LowStockCount = await activeProducts.CountAsync(product => product.QuantityInStock <= product.ReorderLevel),
            OutOfStockCount = await activeProducts.CountAsync(product => product.QuantityInStock <= 0),
            TodaySalesCount = await todaySales.CountAsync(),
            TodaySalesAmount = await todaySales.Select(sale => (decimal?)sale.TotalAmount).SumAsync() ?? 0m,
            TodayPurchaseCount = await todayPurchases.CountAsync(),
            TodayPurchaseAmount = await todayPurchases.Select(purchase => (decimal?)purchase.TotalAmount).SumAsync() ?? 0m,
            RecentSales = await GetRecentSalesAsync(),
            RecentPurchases = await GetRecentPurchasesAsync(),
            LowStockProducts = lowStock.Take(LowStockPreviewCount).ToList()
        };
    }

    private async Task<List<DashboardActivityDto>> GetRecentSalesAsync()
    {
        return await _context.Sales
            .AsNoTracking()
            .OrderByDescending(sale => sale.SaleDate)
            .ThenByDescending(sale => sale.Id)
            .Take(RecentActivityCount)
            .Select(sale => new DashboardActivityDto
            {
                Id = sale.Id,
                Number = sale.SaleNumber,
                PartyName = sale.CustomerName ?? "Walk-in customer",
                TotalAmount = sale.TotalAmount,
                TotalQuantity = sale.Items.Sum(item => item.Quantity),
                Status = sale.Status.ToString(),
                OccurredAt = sale.SaleDate
            })
            .ToListAsync();
    }

    private async Task<List<DashboardActivityDto>> GetRecentPurchasesAsync()
    {
        return await _context.Purchases
            .AsNoTracking()
            .OrderByDescending(purchase => purchase.PurchaseDate)
            .ThenByDescending(purchase => purchase.Id)
            .Take(RecentActivityCount)
            .Select(purchase => new DashboardActivityDto
            {
                Id = purchase.Id,
                Number = purchase.PurchaseNumber,
                PartyName = purchase.Supplier!.Name,
                TotalAmount = purchase.TotalAmount,
                TotalQuantity = purchase.Items.Sum(item => item.Quantity),
                Status = purchase.Status.ToString(),
                OccurredAt = purchase.PurchaseDate
            })
            .ToListAsync();
    }
}
