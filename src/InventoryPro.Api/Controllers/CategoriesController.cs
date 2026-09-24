using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Categories;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPro.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Lists categories. All authenticated users.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> GetAll(
        [FromQuery] bool includeInactive = false)
    {
        var categories = await _categoryService.GetAllAsync(includeInactive);

        return Ok(ApiResponse<IReadOnlyList<CategoryDto>>.Ok(categories));
    }

    /// <summary>Gets one category by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);

        return Ok(ApiResponse<CategoryDto>.Ok(category));
    }

    /// <summary>Creates a category. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create(
        [FromBody] CreateCategoryRequestDto request)
    {
        var category = await _categoryService.CreateAsync(request);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<CategoryDto>.Ok(category, "Category created.", StatusCodes.Status201Created));
    }

    /// <summary>Updates a category. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(
        int id,
        [FromBody] UpdateCategoryRequestDto request)
    {
        var category = await _categoryService.UpdateAsync(id, request);

        return Ok(ApiResponse<CategoryDto>.Ok(category, "Category updated."));
    }

    /// <summary>Soft-deletes a category. Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _categoryService.DeleteAsync(id);

        return Ok(ApiResponse.Ok("Category deleted."));
    }
}
