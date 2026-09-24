using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Purchases;

namespace InventoryPro.Api.Services.Interfaces;

/// <summary>Stock-In (purchase) use cases.</summary>
public interface IPurchaseService
{
    Task<PagedResult<PurchaseDto>> GetPagedAsync(PurchaseQueryParameters parameters);

    Task<PurchaseDto> GetByIdAsync(int id);

    Task<PurchaseDto> CreateAsync(CreatePurchaseRequestDto request, int userId);
}
