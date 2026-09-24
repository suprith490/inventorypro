using InventoryPro.Api.DTOs.Categories;

namespace InventoryPro.Api.Services.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive);

    Task<CategoryDto> GetByIdAsync(int id);

    Task<CategoryDto> CreateAsync(CreateCategoryRequestDto request);

    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequestDto request);

    Task DeleteAsync(int id);
}
