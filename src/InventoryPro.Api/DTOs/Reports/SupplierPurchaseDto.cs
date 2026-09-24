namespace InventoryPro.Api.DTOs.Reports;

/// <summary>Purchase spend grouped by supplier.</summary>
public class SupplierPurchaseDto
{
    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public int OrderCount { get; set; }

    public int UnitsReceived { get; set; }

    public decimal TotalSpend { get; set; }
}
