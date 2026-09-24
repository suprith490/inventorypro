using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Dashboard;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

/// <summary>
/// Aggregated summary endpoints that power the Admin and Staff dashboards.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Full business overview. Admin only.</summary>
    [HttpGet("admin")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetAdminDashboard()
    {
        var dashboard = await _dashboardService.GetAdminDashboardAsync();

        return Ok(ApiResponse<AdminDashboardDto>.Ok(dashboard));
    }

    /// <summary>Operational overview for store staff. Admin and Staff.</summary>
    [HttpGet("staff")]
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    [ProducesResponseType(typeof(ApiResponse<StaffDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StaffDashboardDto>>> GetStaffDashboard()
    {
        var dashboard = await _dashboardService.GetStaffDashboardAsync();

        return Ok(ApiResponse<StaffDashboardDto>.Ok(dashboard));
    }
}
