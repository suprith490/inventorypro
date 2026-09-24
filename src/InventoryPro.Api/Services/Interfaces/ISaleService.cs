using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Sales;

namespace InventoryPro.Api.Services.Interfaces;

/// <summary>Stock-Out (sales) use cases.</summary>
public interface ISaleService
{
    Task<PagedResult<SaleDto>> GetPagedAsync(SaleQueryParameters parameters);

    Task<SaleDto> GetByIdAsync(int id);

    Task<SaleDto> CreateAsync(CreateSaleRequestDto request, int userId);
}
