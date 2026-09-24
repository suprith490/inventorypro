using InventoryPro.Api.DTOs.Inventory;

namespace InventoryPro.Api.DTOs.Dashboard;

/// <summary>Summary shown on the Admin landing page.</summary>
public class AdminDashboardDto
{
    public int TotalProducts { get; set; }

    public int ActiveProducts { get; set; }

    public int TotalCategories { get; set; }

    public int TotalSuppliers { get; set; }

    public int TotalUsers { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }

    public int TotalUnits { get; set; }

    public decimal TotalStockCostValue { get; set; }

    public decimal TotalStockRetailValue { get; set; }

    public decimal TodaySalesAmount { get; set; }

    public int TodaySalesCount { get; set; }

    public decimal TodayPurchaseAmount { get; set; }

    public int TodayPurchaseCount { get; set; }

    public decimal MonthSalesAmount { get; set; }

    public decimal MonthPurchaseAmount { get; set; }

    public IReadOnlyList<DashboardActivityDto> RecentSales { get; set; } = Array.Empty<DashboardActivityDto>();

    public IReadOnlyList<DashboardActivityDto> RecentPurchases { get; set; } = Array.Empty<DashboardActivityDto>();

    public IReadOnlyList<LowStockProductDto> LowStockProducts { get; set; } = Array.Empty<LowStockProductDto>();
}
