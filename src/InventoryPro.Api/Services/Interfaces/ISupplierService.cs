using InventoryPro.Api.DTOs.Suppliers;

namespace InventoryPro.Api.Services.Interfaces;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierDto>> GetAllAsync(bool includeInactive);

    Task<SupplierDto> GetByIdAsync(int id);

    Task<SupplierDto> CreateAsync(CreateSupplierRequestDto request);

    Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequestDto request);

    Task DeleteAsync(int id);
}
