using InventoryPro.Api.DTOs.Reports;

namespace InventoryPro.Api.Services.Interfaces;

/// <summary>Read-only aggregated reports for dashboards and export screens.</summary>
public interface IReportService
{
    Task<InventoryValuationReportDto> GetInventoryValuationAsync();

    Task<SalesReportDto> GetSalesReportAsync(DateTime? from, DateTime? to);

    Task<PurchaseReportDto> GetPurchaseReportAsync(DateTime? from, DateTime? to);
}
