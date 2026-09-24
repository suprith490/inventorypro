using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Reports;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

/// <summary>
/// Aggregated inventory, sales and purchase reports.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Current stock valuation by category. Admin only.</summary>
    [HttpGet("inventory-valuation")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<InventoryValuationReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InventoryValuationReportDto>>> GetInventoryValuation()
    {
        var report = await _reportService.GetInventoryValuationAsync();

        return Ok(ApiResponse<InventoryValuationReportDto>.Ok(report));
    }

    /// <summary>
    /// Sales report for an optional date range (inclusive). Admin and Staff.
    /// Example: /api/reports/sales?from=2026-09-01&amp;to=2026-09-30
    /// </summary>
    [HttpGet("sales")]
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    [ProducesResponseType(typeof(ApiResponse<SalesReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SalesReportDto>>> GetSalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var report = await _reportService.GetSalesReportAsync(from, to);

        return Ok(ApiResponse<SalesReportDto>.Ok(report));
    }

    /// <summary>
    /// Purchase (spend) report for an optional date range (inclusive). Admin and Staff.
    /// Example: /api/reports/purchases?from=2026-09-01&amp;to=2026-09-30
    /// </summary>
    [HttpGet("purchases")]
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PurchaseReportDto>>> GetPurchaseReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var report = await _reportService.GetPurchaseReportAsync(from, to);

        return Ok(ApiResponse<PurchaseReportDto>.Ok(report));
    }
}
