using InventoryPro.Api.Entities;

namespace InventoryPro.Api.Services.Interfaces;

/// <summary>
/// Successful token generation result: the raw JWT and its expiry.
/// </summary>
public record JwtTokenResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(User user);
}
