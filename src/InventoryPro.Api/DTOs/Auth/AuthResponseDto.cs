namespace InventoryPro.Api.DTOs.Auth;

/// <summary>
/// Returned by register and login. The frontend stores Token and sends it as
/// "Authorization: Bearer &lt;token&gt;" on every subsequent request.
/// </summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public UserProfileDto User { get; set; } = new();
}
