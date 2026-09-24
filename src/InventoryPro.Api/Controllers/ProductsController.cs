using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Products;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Lists products with search, filtering, sorting and pagination. All authenticated users.
    /// Example: /api/products?search=mouse&amp;categoryId=1&amp;lowStockOnly=true&amp;sortBy=price&amp;sortDescending=true&amp;page=1&amp;pageSize=10
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetAll(
        [FromQuery] ProductQueryParameters parameters)
    {
        var products = await _productService.GetPagedAsync(parameters);

        return Ok(ApiResponse<PagedResult<ProductDto>>.Ok(products));
    }

    /// <summary>Gets one product by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    /// <summary>Creates a product. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create(
        [FromBody] CreateProductRequestDto request)
    {
        var product = await _productService.CreateAsync(request);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<ProductDto>.Ok(product, "Product created.", StatusCodes.Status201Created));
    }

    /// <summary>Updates a product's catalog data. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(
        int id,
        [FromBody] UpdateProductRequestDto request)
    {
        var product = await _productService.UpdateAsync(id, request);

        return Ok(ApiResponse<ProductDto>.Ok(product, "Product updated."));
    }

    /// <summary>Soft-deletes a product. Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _productService.DeleteAsync(id);

        return Ok(ApiResponse.Ok("Product deleted."));
    }
}
