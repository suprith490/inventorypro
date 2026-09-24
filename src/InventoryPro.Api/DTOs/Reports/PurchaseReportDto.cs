namespace InventoryPro.Api.DTOs.Reports;

/// <summary>Purchasing spend for an optional date range.</summary>
public class PurchaseReportDto
{
    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public int TotalOrders { get; set; }

    public int TotalUnitsReceived { get; set; }

    public decimal TotalSpend { get; set; }

    public IReadOnlyList<SupplierPurchaseDto> BySupplier { get; set; } = Array.Empty<SupplierPurchaseDto>();
}
