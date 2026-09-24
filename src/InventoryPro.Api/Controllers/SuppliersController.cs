using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Suppliers;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>Lists suppliers. All authenticated users.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SupplierDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SupplierDto>>>> GetAll(
        [FromQuery] bool includeInactive = false)
    {
        var suppliers = await _supplierService.GetAllAsync(includeInactive);

        return Ok(ApiResponse<IReadOnlyList<SupplierDto>>.Ok(suppliers));
    }

    /// <summary>Gets one supplier by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> GetById(int id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);

        return Ok(ApiResponse<SupplierDto>.Ok(supplier));
    }

    /// <summary>Creates a supplier. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Create(
        [FromBody] CreateSupplierRequestDto request)
    {
        var supplier = await _supplierService.CreateAsync(request);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<SupplierDto>.Ok(supplier, "Supplier created.", StatusCodes.Status201Created));
    }

    /// <summary>Updates a supplier. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Update(
        int id,
        [FromBody] UpdateSupplierRequestDto request)
    {
        var supplier = await _supplierService.UpdateAsync(id, request);

        return Ok(ApiResponse<SupplierDto>.Ok(supplier, "Supplier updated."));
    }

    /// <summary>Soft-deletes a supplier. Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _supplierService.DeleteAsync(id);

        return Ok(ApiResponse.Ok("Supplier deleted."));
    }
}
