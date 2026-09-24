using InventoryPro.Api.Common;
using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.Data;
using InventoryPro.Api.DTOs.Products;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Services.Implementations;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(ProductQueryParameters parameters)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Supplier)
            .AsQueryable();

        if (!parameters.IncludeInactive)
        {
            query = query.Where(product => product.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            // ToLower on both sides keeps search case-insensitive on every provider
            // (SQL Server collations are case-insensitive but SQLite is not).
            var term = parameters.Search.Trim().ToLowerInvariant();
            query = query.Where(product =>
                product.Name.ToLower().Contains(term) ||
                product.SKU.ToLower().Contains(term) ||
                (product.Description != null && product.Description.ToLower().Contains(term)));
        }

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == parameters.CategoryId.Value);
        }

        if (parameters.SupplierId.HasValue)
        {
            query = query.Where(product => product.SupplierId == parameters.SupplierId.Value);
        }

        if (parameters.LowStockOnly)
        {
            query = query.Where(product => product.QuantityInStock <= product.ReorderLevel);
        }

        // Whitelisted sort columns keep this safe from arbitrary SQL-ish input.
        var descending = parameters.SortDescending;
        query = (parameters.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "sku" => Order(query, product => product.SKU, descending),
            "price" => Order(query, product => product.UnitPrice, descending),
            "stock" => Order(query, product => product.QuantityInStock, descending),
            "created" => Order(query, product => product.CreatedAt, descending),
            _ => Order(query, product => product.Name, descending)
        };

        var totalCount = await query.CountAsync();

        var products = await query
            .Skip(parameters.Skip)
            .Take(parameters.PageSize)
            .ToListAsync();

        return PagedResult<ProductDto>.Create(
            products.Select(MapToDto).ToList(),
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }

    /// <summary>
    /// Small helper so we can choose sort direction without duplicating the
    /// whole query pipeline (EF Core builds an IOrderedQueryable from either lambda).
    /// </summary>
    private static IQueryable<Product> Order<TKey>(
        IQueryable<Product> query,
        System.Linq.Expressions.Expression<Func<Product, TKey>> keySelector,
        bool descending)
    {
        var ordered = descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
        // Secondary sort on Id keeps paging stable when keys are equal.
        return descending
            ? ordered.ThenByDescending(product => product.Id)
            : ordered.ThenBy(product => product.Id);
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(candidate => candidate.Category)
            .Include(candidate => candidate.Supplier)
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (product is null)
        {
            throw new NotFoundException($"Product with id {id} was not found.");
        }

        return MapToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequestDto request)
    {
        var sku = NormalizeSku(request.SKU);

        var skuAlreadyUsed = await _context.Products
            .AnyAsync(product => product.SKU == sku);

        if (skuAlreadyUsed)
        {
            throw new ConflictException($"A product with SKU '{sku}' already exists.");
        }

        await EnsureCategoryExistsAsync(request.CategoryId);
        await EnsureSupplierExistsAsync(request.SupplierId);

        var product = new Product
        {
            SKU = sku,
            Name = request.Name.Trim(),
            Description = NormalizeText(request.Description),
            CategoryId = request.CategoryId,
            SupplierId = request.SupplierId,
            UnitPrice = request.UnitPrice,
            CostPrice = request.CostPrice,
            // New products start with zero stock; stock enters through purchases
            // or inventory adjustments so every movement is audited.
            QuantityInStock = 0,
            ReorderLevel = request.ReorderLevel,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequestDto request)
    {
        var product = await _context.Products
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (product is null)
        {
            throw new NotFoundException($"Product with id {id} was not found.");
        }

        var sku = NormalizeSku(request.SKU);

        var skuAlreadyUsed = await _context.Products
            .AnyAsync(candidate => candidate.SKU == sku && candidate.Id != id);

        if (skuAlreadyUsed)
        {
            throw new ConflictException($"A product with SKU '{sku}' already exists.");
        }

        await EnsureCategoryExistsAsync(request.CategoryId);
        await EnsureSupplierExistsAsync(request.SupplierId);

        product.SKU = sku;
        product.Name = request.Name.Trim();
        product.Description = NormalizeText(request.Description);
        product.CategoryId = request.CategoryId;
        product.SupplierId = request.SupplierId;
        product.UnitPrice = request.UnitPrice;
        product.CostPrice = request.CostPrice;
        product.ReorderLevel = request.ReorderLevel;
        product.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(product.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.Products
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (product is null)
        {
            throw new NotFoundException($"Product with id {id} was not found.");
        }

        // Soft delete keeps purchase/sale history intact.
        product.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        var exists = await _context.Categories
            .AnyAsync(category => category.Id == categoryId && category.IsActive);

        if (!exists)
        {
            throw new BadRequestException($"Category with id {categoryId} does not exist or is inactive.");
        }
    }

    private async Task EnsureSupplierExistsAsync(int? supplierId)
    {
        if (supplierId is null)
        {
            return;
        }

        var exists = await _context.Suppliers
            .AnyAsync(supplier => supplier.Id == supplierId && supplier.IsActive);

        if (!exists)
        {
            throw new BadRequestException($"Supplier with id {supplierId} does not exist or is inactive.");
        }
    }

    private static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        SKU = product.SKU,
        Name = product.Name,
        Description = product.Description,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty,
        SupplierId = product.SupplierId,
        SupplierName = product.Supplier?.Name,
        UnitPrice = product.UnitPrice,
        CostPrice = product.CostPrice,
        QuantityInStock = product.QuantityInStock,
        ReorderLevel = product.ReorderLevel,
        IsLowStock = product.QuantityInStock <= product.ReorderLevel,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}
