using System.Security.Claims;
using InventoryPro.Api.Services.Interfaces;

namespace InventoryPro.Api.Services.Implementations;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public int? UserId
    {
        get
        {
            var rawUserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User?.FindFirst("sub")?.Value;

            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value
                            ?? User?.FindFirst("email")?.Value;

    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value
                           ?? User?.FindFirst("role")?.Value;
}
