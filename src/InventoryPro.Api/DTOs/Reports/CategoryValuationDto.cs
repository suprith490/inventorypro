namespace InventoryPro.Api.DTOs.Reports;

/// <summary>Inventory value grouped by category.</summary>
public class CategoryValuationDto
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int ProductCount { get; set; }

    public int TotalUnits { get; set; }

    public decimal CostValue { get; set; }

    public decimal RetailValue { get; set; }
}
