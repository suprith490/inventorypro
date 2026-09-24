using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Auth;
using InventoryPro.Api.DTOs.Users;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

/// <summary>Admin-only user management.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Lists all users.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserProfileDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserProfileDto>>>> GetAll()
    {
        var users = await _userService.GetAllAsync();

        return Ok(ApiResponse<IReadOnlyList<UserProfileDto>>.Ok(users));
    }

    /// <summary>Gets one user by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);

        return Ok(ApiResponse<UserProfileDto>.Ok(user));
    }

    /// <summary>Promotes or demotes a user.</summary>
    [HttpPut("{id:int}/role")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateRole(
        int id,
        [FromBody] UpdateUserRoleRequestDto request)
    {
        var user = await _userService.UpdateRoleAsync(id, request.Role);

        return Ok(ApiResponse<UserProfileDto>.Ok(user, "User role updated."));
    }

    /// <summary>Activates or deactivates a user.</summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateStatus(
        int id,
        [FromBody] UpdateUserStatusRequestDto request)
    {
        var user = await _userService.UpdateStatusAsync(id, request.IsActive);

        return Ok(ApiResponse<UserProfileDto>.Ok(user, "User status updated."));
    }
}
