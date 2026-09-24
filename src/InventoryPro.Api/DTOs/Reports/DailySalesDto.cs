namespace InventoryPro.Api.DTOs.Reports;

/// <summary>One day of the sales report trend.</summary>
public class DailySalesDto
{
    public DateTime Date { get; set; }

    public int OrderCount { get; set; }

    public int UnitsSold { get; set; }

    public decimal Revenue { get; set; }
}
