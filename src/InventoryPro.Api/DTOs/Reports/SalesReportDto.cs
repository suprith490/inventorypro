namespace InventoryPro.Api.DTOs.Reports;

/// <summary>Sales performance for an optional date range.</summary>
public class SalesReportDto
{
    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public int TotalOrders { get; set; }

    public int TotalUnitsSold { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal TotalCostOfGoods { get; set; }

    public decimal GrossProfit { get; set; }

    public IReadOnlyList<DailySalesDto> DailyBreakdown { get; set; } = Array.Empty<DailySalesDto>();

    public IReadOnlyList<TopSellingProductDto> TopProducts { get; set; } = Array.Empty<TopSellingProductDto>();
}
