using InventoryPro.Api.Common;
using InventoryPro.Api.DTOs.Inventory;

namespace InventoryPro.Api.Services.Interfaces;

/// <summary>Inventory ledger, stock adjustments and low-stock alerts.</summary>
public interface IInventoryService
{
    Task<PagedResult<InventoryTransactionDto>> GetTransactionsAsync(InventoryQueryParameters parameters);

    Task<InventoryTransactionDto> AdjustStockAsync(AdjustStockRequestDto request, int userId);

    Task<IReadOnlyList<LowStockProductDto>> GetLowStockAsync();
}
