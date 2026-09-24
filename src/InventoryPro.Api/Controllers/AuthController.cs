using InventoryPro.Api.Common;
using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.DTOs.Auth;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

/// <summary>
/// Registration, login and profile endpoints.
/// Mirrors a Spring Boot AuthController with AuthenticationManager + JwtTokenProvider.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    /// <summary>Creates a new Staff account and returns a JWT.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<AuthResponseDto>.Ok(result, "Registration successful.", StatusCodes.Status201Created));
    }

    /// <summary>Authenticates a user and returns a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);

        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
    }

    /// <summary>Returns the profile of the authenticated caller.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> Me()
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("The authenticated user id was not found in the token.");

        var profile = await _authService.GetProfileAsync(userId);

        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    /// <summary>
    /// Diagnostic endpoint used to prove role-based authorization works.
    /// Only callers whose token contains the Admin role can reach it.
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<ApiResponse<string>> AdminOnly()
    {
        return Ok(ApiResponse<string>.Ok("You have Admin access.", "Authorized."));
    }
}
