using InventoryPro.Api.DTOs.Dashboard;

namespace InventoryPro.Api.Services.Interfaces;

/// <summary>Aggregated numbers for the Admin and Staff landing pages.</summary>
public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboardAsync();

    Task<StaffDashboardDto> GetStaffDashboardAsync();
}
