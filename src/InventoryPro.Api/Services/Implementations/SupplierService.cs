using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.Data;
using InventoryPro.Api.DTOs.Suppliers;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Services.Implementations;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _context;

    public SupplierService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SupplierDto>> GetAllAsync(bool includeInactive)
    {
        var query = _context.Suppliers.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(supplier => supplier.IsActive);
        }

        var suppliers = await query
            .OrderBy(supplier => supplier.Name)
            .ToListAsync();

        return suppliers.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto> GetByIdAsync(int id)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (supplier is null)
        {
            throw new NotFoundException($"Supplier with id {id} was not found.");
        }

        return MapToDto(supplier);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequestDto request)
    {
        var name = request.Name.Trim();

        var nameAlreadyUsed = await _context.Suppliers
            .AnyAsync(supplier => supplier.Name == name);

        if (nameAlreadyUsed)
        {
            throw new ConflictException($"A supplier named '{name}' already exists.");
        }

        var supplier = new Supplier
        {
            Name = name,
            Email = Normalize(request.Email),
            Phone = Normalize(request.Phone),
            Address = Normalize(request.Address),
            IsActive = true
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return MapToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequestDto request)
    {
        var supplier = await _context.Suppliers
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (supplier is null)
        {
            throw new NotFoundException($"Supplier with id {id} was not found.");
        }

        var name = request.Name.Trim();

        var nameAlreadyUsed = await _context.Suppliers
            .AnyAsync(candidate => candidate.Name == name && candidate.Id != id);

        if (nameAlreadyUsed)
        {
            throw new ConflictException($"A supplier named '{name}' already exists.");
        }

        supplier.Name = name;
        supplier.Email = Normalize(request.Email);
        supplier.Phone = Normalize(request.Phone);
        supplier.Address = Normalize(request.Address);
        supplier.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return MapToDto(supplier);
    }

    public async Task DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (supplier is null)
        {
            throw new NotFoundException($"Supplier with id {id} was not found.");
        }

        var isLinkedToProducts = await _context.Products
            .AnyAsync(product => product.SupplierId == id && product.IsActive);

        if (isLinkedToProducts)
        {
            throw new ConflictException(
                $"Supplier '{supplier.Name}' cannot be deleted because it supplies active products.");
        }

        var hasPurchaseHistory = await _context.Purchases
            .AnyAsync(purchase => purchase.SupplierId == id);

        if (hasPurchaseHistory)
        {
            throw new ConflictException(
                $"Supplier '{supplier.Name}' cannot be deleted because it has purchase history.");
        }

        supplier.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SupplierDto MapToDto(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        Email = supplier.Email,
        Phone = supplier.Phone,
        Address = supplier.Address,
        IsActive = supplier.IsActive,
        CreatedAt = supplier.CreatedAt
    };
}
