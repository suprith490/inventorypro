using System.Security.Claims;

namespace InventoryPro.Api.Services.Interfaces;

/// <summary>
/// Reads the identity of the caller from the current HTTP request.
/// In Spring Boot you would read this from SecurityContextHolder.getContext().
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
