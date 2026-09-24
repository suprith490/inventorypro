using InventoryPro.Api.DTOs.Auth;
using InventoryPro.Api.Entities;

namespace InventoryPro.Api.Services.Interfaces;

/// <summary>Admin-only user management.</summary>
public interface IUserService
{
    Task<IReadOnlyList<UserProfileDto>> GetAllAsync();

    Task<UserProfileDto> GetByIdAsync(int id);

    Task<UserProfileDto> UpdateRoleAsync(int id, UserRole role);

    Task<UserProfileDto> UpdateStatusAsync(int id, bool isActive);
}
