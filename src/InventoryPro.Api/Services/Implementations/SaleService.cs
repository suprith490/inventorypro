using InventoryPro.Api.Common;
using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.Data;
using InventoryPro.Api.DTOs.Sales;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Services.Implementations;

/// <summary>
/// Records Stock-Out (sales) documents. Creating a sale:
///   1. validates every product line and that enough stock is on hand,
///   2. writes the Sale + SaleItems,
///   3. decreases each product's QuantityInStock,
///   4. appends one InventoryTransaction per line for the audit trail.
/// Runs in a single database transaction; any failure rolls back the whole sale.
/// </summary>
public class SaleService : ISaleService
{
    private readonly AppDbContext _context;

    public SaleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<SaleDto>> GetPagedAsync(SaleQueryParameters parameters)
    {
        var query = _context.Sales
            .AsNoTracking()
            .Include(sale => sale.User)
            .Include(sale => sale.Items)
                .ThenInclude(item => item.Product)
            .AsQueryable();

        if (parameters.UserId.HasValue)
        {
            query = query.Where(sale => sale.UserId == parameters.UserId.Value);
        }

        if (parameters.From.HasValue)
        {
            query = query.Where(sale => sale.SaleDate >= parameters.From.Value);
        }

        if (parameters.To.HasValue)
        {
            var exclusiveEnd = parameters.To.Value.Date.AddDays(1);
            query = query.Where(sale => sale.SaleDate < exclusiveEnd);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.Trim().ToLowerInvariant();
            query = query.Where(sale =>
                sale.SaleNumber.ToLower().Contains(term) ||
                (sale.CustomerName != null && sale.CustomerName.ToLower().Contains(term)) ||
                sale.User!.FullName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        query = parameters.SortDescending
            ? query.OrderByDescending(sale => sale.SaleDate).ThenByDescending(sale => sale.Id)
            : query.OrderBy(sale => sale.SaleDate).ThenBy(sale => sale.Id);

        var sales = await query
            .Skip(parameters.Skip)
            .Take(parameters.PageSize)
            .ToListAsync();

        return PagedResult<SaleDto>.Create(
            sales.Select(MapToDto).ToList(),
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }

    public async Task<SaleDto> GetByIdAsync(int id)
    {
        var sale = await _context.Sales
            .AsNoTracking()
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.Items)
                .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (sale is null)
        {
            throw new NotFoundException($"Sale with id {id} was not found.");
        }

        return MapToDto(sale);
    }

    public async Task<SaleDto> CreateAsync(CreateSaleRequestDto request, int userId)
    {
        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();

        var products = await _context.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                throw new BadRequestException($"Product with id {item.ProductId} was not found.");
            }

            if (!product.IsActive)
            {
                throw new BadRequestException(
                    $"Product '{product.Name}' is inactive and cannot be sold.");
            }

            if (product.QuantityInStock < item.Quantity)
            {
                throw new ConflictException(
                    $"Insufficient stock for '{product.Name}' (SKU {product.SKU}). " +
                    $"Requested {item.Quantity}, available {product.QuantityInStock}.");
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var saleDate = request.SaleDate ?? DateTime.UtcNow;

            var sale = new Sale
            {
                SaleNumber = await GenerateSaleNumberAsync(saleDate),
                UserId = userId,
                SaleDate = saleDate,
                Status = SaleStatus.Completed,
                CustomerName = NormalizeText(request.CustomerName),
                Notes = NormalizeText(request.Notes),
                Items = new List<SaleItem>()
            };

            var ledgerEntries = new List<InventoryTransaction>();
            decimal totalAmount = 0m;

            foreach (var item in request.Items)
            {
                var product = products[item.ProductId];
                var unitPrice = item.UnitPrice ?? product.UnitPrice;
                var lineTotal = Math.Round(unitPrice * item.Quantity, 2);
                totalAmount += lineTotal;

                sale.Items.Add(new SaleItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    LineTotal = lineTotal
                });

                var quantityBefore = product.QuantityInStock;
                product.QuantityInStock = quantityBefore - item.Quantity;

                ledgerEntries.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    UserId = userId,
                    TransactionType = TransactionType.StockOut,
                    Quantity = item.Quantity,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = product.QuantityInStock,
                    ReferenceType = ReferenceType.Sale,
                    Notes = $"Stock out via sale {sale.SaleNumber}"
                });
            }

            sale.TotalAmount = totalAmount;
            _context.Sales.Add(sale);

            await _context.SaveChangesAsync();

            foreach (var entry in ledgerEntries)
            {
                entry.ReferenceId = sale.Id;
                _context.InventoryTransactions.Add(entry);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdAsync(sale.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<string> GenerateSaleNumberAsync(DateTime date)
    {
        var prefix = $"SO-{date:yyyyMMdd}-";

        var existingNumbers = await _context.Sales
            .Where(sale => sale.SaleNumber.StartsWith(prefix))
            .Select(sale => sale.SaleNumber)
            .ToListAsync();

        var next = existingNumbers.Count + 1;
        var candidate = $"{prefix}{next:D4}";

        while (existingNumbers.Contains(candidate))
        {
            next++;
            candidate = $"{prefix}{next:D4}";
        }

        return candidate;
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SaleDto MapToDto(Sale sale) => new()
    {
        Id = sale.Id,
        SaleNumber = sale.SaleNumber,
        UserId = sale.UserId,
        UserName = sale.User?.FullName ?? string.Empty,
        SaleDate = sale.SaleDate,
        TotalAmount = sale.TotalAmount,
        Status = sale.Status.ToString(),
        CustomerName = sale.CustomerName,
        Notes = sale.Notes,
        TotalQuantity = sale.Items.Sum(item => item.Quantity),
        CreatedAt = sale.CreatedAt,
        Items = sale.Items
            .OrderBy(item => item.Id)
            .Select(item => new SaleItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? string.Empty,
                SKU = item.Product?.SKU ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            })
            .ToList()
    };
}
