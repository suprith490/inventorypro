using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Products;

namespace InventoryPro.Api.Services.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductQueryParameters parameters);

    Task<ProductDto> GetByIdAsync(int id);

    Task<ProductDto> CreateAsync(CreateProductRequestDto request);

    Task<ProductDto> UpdateAsync(int id, UpdateProductRequestDto request);

    Task DeleteAsync(int id);
}
