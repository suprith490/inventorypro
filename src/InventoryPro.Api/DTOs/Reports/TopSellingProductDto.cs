namespace InventoryPro.Api.DTOs.Reports;

/// <summary>Best-selling product within the report period.</summary>
public class TopSellingProductDto
{
    public int ProductId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int UnitsSold { get; set; }

    public decimal Revenue { get; set; }
}
