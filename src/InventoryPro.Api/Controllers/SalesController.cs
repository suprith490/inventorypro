using InventoryPro.Api.Common;
using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.DTOs.Sales;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

/// <summary>
/// Stock-Out (sales to customers). Admin and Staff can record and view them.
/// </summary>
[ApiController]
[Route("api/sales")]
[Authorize(Roles = AppRoles.AdminOrStaff)]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly ICurrentUserService _currentUserService;

    public SalesController(
        ISaleService saleService,
        ICurrentUserService currentUserService)
    {
        _saleService = saleService;
        _currentUserService = currentUserService;
    }

    /// <summary>Paginated sales history with date/user/search filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SaleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<SaleDto>>>> GetAll(
        [FromQuery] SaleQueryParameters parameters)
    {
        var sales = await _saleService.GetPagedAsync(parameters);

        return Ok(ApiResponse<PagedResult<SaleDto>>.Ok(sales));
    }

    /// <summary>Gets one sale with its line items.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SaleDto>>> GetById(int id)
    {
        var sale = await _saleService.GetByIdAsync(id);

        return Ok(ApiResponse<SaleDto>.Ok(sale));
    }

    /// <summary>
    /// Records a Stock-Out document. Validates stock, decreases inventory and
    /// writes inventory ledger entries. Returns 409 when stock is insufficient.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SaleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<SaleDto>>> Create(
        [FromBody] CreateSaleRequestDto request)
    {
        var userId = GetUserId();

        var sale = await _saleService.CreateAsync(request, userId);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<SaleDto>.Ok(sale, "Sale recorded.", StatusCodes.Status201Created));
    }

    private int GetUserId()
    {
        return _currentUserService.UserId
            ?? throw new UnauthorizedException("The authenticated user id was not found in the token.");
    }
}
