namespace InventoryPro.Api.DTOs.Reports;

/// <summary>
/// Current value of everything on hand, broken down by category.
/// Useful for balance-sheet style stock valuation.
/// </summary>
public class InventoryValuationReportDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public int ProductCount { get; set; }

    public int TotalUnits { get; set; }

    public decimal TotalCostValue { get; set; }

    public decimal TotalRetailValue { get; set; }

    public decimal PotentialProfit { get; set; }

    public IReadOnlyList<CategoryValuationDto> Categories { get; set; } = Array.Empty<CategoryValuationDto>();
}
