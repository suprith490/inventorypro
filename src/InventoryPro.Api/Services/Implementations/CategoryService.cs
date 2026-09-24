using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.Data;
using InventoryPro.Api.DTOs.Categories;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive)
    {
        var query = _context.Categories.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(category => category.IsActive);
        }

        var categories = await query
            .OrderBy(category => category.Name)
            .ToListAsync();

        return categories.Select(MapToDto).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (category is null)
        {
            throw new NotFoundException($"Category with id {id} was not found.");
        }

        return MapToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequestDto request)
    {
        var name = request.Name.Trim();

        var nameAlreadyUsed = await _context.Categories
            .AnyAsync(category => category.Name == name);

        if (nameAlreadyUsed)
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        var category = new Category
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = true
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequestDto request)
    {
        var category = await _context.Categories
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (category is null)
        {
            throw new NotFoundException($"Category with id {id} was not found.");
        }

        var name = request.Name.Trim();

        var nameAlreadyUsed = await _context.Categories
            .AnyAsync(candidate => candidate.Name == name && candidate.Id != id);

        if (nameAlreadyUsed)
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        category.Name = name;
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _context.Categories
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (category is null)
        {
            throw new NotFoundException($"Category with id {id} was not found.");
        }

        var isInUse = await _context.Products
            .AnyAsync(product => product.CategoryId == id && product.IsActive);

        if (isInUse)
        {
            throw new ConflictException(
                $"Category '{category.Name}' cannot be deleted because it is assigned to active products.");
        }

        // Soft delete: keep the row for historical references.
        category.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private static CategoryDto MapToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt
    };
}
