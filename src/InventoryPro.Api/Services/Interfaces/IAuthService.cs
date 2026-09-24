using InventoryPro.Api.DTOs.Auth;

namespace InventoryPro.Api.Services.Interfaces;

/// <summary>
/// Authentication use cases: register, login, read profile.
/// </summary>
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);

    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

    Task<UserProfileDto> GetProfileAsync(int userId);
}
