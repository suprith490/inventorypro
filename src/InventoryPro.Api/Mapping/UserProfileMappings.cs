using InventoryPro.Api.DTOs.Auth;
using InventoryPro.Api.Entities;

namespace InventoryPro.Api.Mapping;

/// <summary>
/// Manual DTO mappings. Kept in one place so every service returns the same shape.
/// In Spring Boot this is often a MapStruct mapper or MapStruct-like helper.
/// </summary>
public static class UserProfileMappings
{
    public static UserProfileDto ToProfileDto(this User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };
}
