using InventoryPro.Api.DTOs.Inventory;

namespace InventoryPro.Api.DTOs.Dashboard;

/// <summary>Summary shown on the Staff landing page (operational focus).</summary>
public class StaffDashboardDto
{
    public int TotalProducts { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }

    public decimal TodaySalesAmount { get; set; }

    public int TodaySalesCount { get; set; }

    public decimal TodayPurchaseAmount { get; set; }

    public int TodayPurchaseCount { get; set; }

    public IReadOnlyList<DashboardActivityDto> RecentSales { get; set; } = Array.Empty<DashboardActivityDto>();

    public IReadOnlyList<DashboardActivityDto> RecentPurchases { get; set; } = Array.Empty<DashboardActivityDto>();

    public IReadOnlyList<LowStockProductDto> LowStockProducts { get; set; } = Array.Empty<LowStockProductDto>();
}
