using InventoryPro.Api.Common;
using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.DTOs.Purchases;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

/// <summary>
/// Stock-In (purchases from suppliers). Admin and Staff can record and view them.
/// </summary>
[ApiController]
[Route("api/purchases")]
[Authorize(Roles = AppRoles.AdminOrStaff)]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;
    private readonly ICurrentUserService _currentUserService;

    public PurchasesController(
        IPurchaseService purchaseService,
        ICurrentUserService currentUserService)
    {
        _purchaseService = purchaseService;
        _currentUserService = currentUserService;
    }

    /// <summary>Paginated purchase history with date/supplier/search filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PurchaseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseDto>>>> GetAll(
        [FromQuery] PurchaseQueryParameters parameters)
    {
        var purchases = await _purchaseService.GetPagedAsync(parameters);

        return Ok(ApiResponse<PagedResult<PurchaseDto>>.Ok(purchases));
    }

    /// <summary>Gets one purchase with its line items.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PurchaseDto>>> GetById(int id)
    {
        var purchase = await _purchaseService.GetByIdAsync(id);

        return Ok(ApiResponse<PurchaseDto>.Ok(purchase));
    }

    /// <summary>
    /// Records a Stock-In document. Increases stock and writes inventory ledger entries.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PurchaseDto>>> Create(
        [FromBody] CreatePurchaseRequestDto request)
    {
        var userId = GetUserId();

        var purchase = await _purchaseService.CreateAsync(request, userId);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<PurchaseDto>.Ok(purchase, "Purchase recorded.", StatusCodes.Status201Created));
    }

    private int GetUserId()
    {
        return _currentUserService.UserId
            ?? throw new UnauthorizedException("The authenticated user id was not found in the token.");
    }
}
