using InventoryPro.Api.Common;
using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.DTOs.Inventory;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

/// <summary>
/// Inventory ledger, low-stock alerts and manual stock adjustments.
/// </summary>
[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUserService;

    public InventoryController(
        IInventoryService inventoryService,
        ICurrentUserService currentUserService)
    {
        _inventoryService = inventoryService;
        _currentUserService = currentUserService;
    }

    /// <summary>Paginated inventory transaction history (audit trail).</summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InventoryTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryTransactionDto>>>> GetTransactions(
        [FromQuery] InventoryQueryParameters parameters)
    {
        var transactions = await _inventoryService.GetTransactionsAsync(parameters);

        return Ok(ApiResponse<PagedResult<InventoryTransactionDto>>.Ok(transactions));
    }

    /// <summary>Products whose stock is at or below their reorder level.</summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LowStockProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LowStockProductDto>>>> GetLowStock()
    {
        var products = await _inventoryService.GetLowStockAsync();

        return Ok(ApiResponse<IReadOnlyList<LowStockProductDto>>.Ok(products));
    }

    /// <summary>
    /// Records a manual stock adjustment (stock count correction, write-off).
    /// Admin and Staff.
    /// </summary>
    [HttpPost("adjust")]
    [Authorize(Roles = AppRoles.AdminOrStaff)]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InventoryTransactionDto>>> Adjust(
        [FromBody] AdjustStockRequestDto request)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("The authenticated user id was not found in the token.");

        var transaction = await _inventoryService.AdjustStockAsync(request, userId);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<InventoryTransactionDto>.Ok(transaction, "Stock adjusted.", StatusCodes.Status201Created));
    }
}
