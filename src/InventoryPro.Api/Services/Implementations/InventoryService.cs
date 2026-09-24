using InventoryPro.Api.Common;
using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.Data;
using InventoryPro.Api.DTOs.Inventory;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Services.Implementations;

/// <summary>
/// Reads the inventory ledger, performs manual stock adjustments and
/// exposes low-stock alerts. The ledger is append-only: rows are never edited
/// or deleted, which keeps the audit trail trustworthy.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<InventoryTransactionDto>> GetTransactionsAsync(
        InventoryQueryParameters parameters)
    {
        var query = _context.InventoryTransactions
            .AsNoTracking()
            .Include(transaction => transaction.Product)
            .Include(transaction => transaction.User)
            .AsQueryable();

        if (parameters.ProductId.HasValue)
        {
            query = query.Where(transaction => transaction.ProductId == parameters.ProductId.Value);
        }

        if (parameters.UserId.HasValue)
        {
            query = query.Where(transaction => transaction.UserId == parameters.UserId.Value);
        }

        if (parameters.TransactionType.HasValue)
        {
            query = query.Where(transaction => transaction.TransactionType == parameters.TransactionType.Value);
        }

        if (parameters.ReferenceType.HasValue)
        {
            query = query.Where(transaction => transaction.ReferenceType == parameters.ReferenceType.Value);
        }

        if (parameters.From.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAt >= parameters.From.Value);
        }

        if (parameters.To.HasValue)
        {
            var exclusiveEnd = parameters.To.Value.Date.AddDays(1);
            query = query.Where(transaction => transaction.CreatedAt < exclusiveEnd);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.Trim().ToLowerInvariant();
            query = query.Where(transaction =>
                transaction.Product!.Name.ToLower().Contains(term) ||
                transaction.Product!.SKU.ToLower().Contains(term) ||
                (transaction.Notes != null && transaction.Notes.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();

        query = parameters.SortDescending
            ? query.OrderByDescending(transaction => transaction.CreatedAt).ThenByDescending(transaction => transaction.Id)
            : query.OrderBy(transaction => transaction.CreatedAt).ThenBy(transaction => transaction.Id);

        var transactions = await query
            .Skip(parameters.Skip)
            .Take(parameters.PageSize)
            .ToListAsync();

        return PagedResult<InventoryTransactionDto>.Create(
            transactions.Select(MapToDto).ToList(),
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }

    public async Task<InventoryTransactionDto> AdjustStockAsync(AdjustStockRequestDto request, int userId)
    {
        var product = await _context.Products
            .SingleOrDefaultAsync(candidate => candidate.Id == request.ProductId);

        if (product is null)
        {
            throw new NotFoundException($"Product with id {request.ProductId} was not found.");
        }

        var quantityBefore = product.QuantityInStock;
        var quantityAfter = request.NewQuantity;
        var delta = quantityAfter - quantityBefore;

        if (delta == 0)
        {
            throw new BadRequestException(
                "The new quantity is the same as the current quantity; no adjustment is needed.");
        }

        product.QuantityInStock = quantityAfter;

        var entry = new InventoryTransaction
        {
            ProductId = product.Id,
            UserId = userId,
            TransactionType = TransactionType.Adjustment,
            // Quantity is always a positive magnitude; direction comes from before/after.
            Quantity = Math.Abs(delta),
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            ReferenceType = ReferenceType.ManualAdjustment,
            Notes = BuildAdjustmentNote(request.Reason, delta)
        };

        _context.InventoryTransactions.Add(entry);
        await _context.SaveChangesAsync();

        // Reload navigation properties for the response.
        var saved = await _context.InventoryTransactions
            .AsNoTracking()
            .Include(transaction => transaction.Product)
            .Include(transaction => transaction.User)
            .SingleAsync(transaction => transaction.Id == entry.Id);

        return MapToDto(saved);
    }

    public async Task<IReadOnlyList<LowStockProductDto>> GetLowStockAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.IsActive && product.QuantityInStock <= product.ReorderLevel)
            .OrderBy(product => product.QuantityInStock - product.ReorderLevel)
            .ThenBy(product => product.Name)
            .ToListAsync();

        return products.Select(MapToLowStockDto).ToList();
    }

    private static string BuildAdjustmentNote(string reason, int delta)
    {
        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? "Manual stock adjustment" : reason.Trim();
        var direction = delta > 0 ? "increased" : "decreased";
        return $"{trimmedReason} ({direction} by {Math.Abs(delta)})";
    }

    internal static LowStockProductDto MapToLowStockDto(Product product) => new()
    {
        ProductId = product.Id,
        SKU = product.SKU,
        Name = product.Name,
        CategoryName = product.Category?.Name ?? string.Empty,
        QuantityInStock = product.QuantityInStock,
        ReorderLevel = product.ReorderLevel,
        // Suggest topping up to twice the reorder level so we don't reorder every day.
        SuggestedReorderQuantity = Math.Max(product.ReorderLevel * 2 - product.QuantityInStock, 1),
        IsOutOfStock = product.QuantityInStock <= 0
    };

    private static InventoryTransactionDto MapToDto(InventoryTransaction transaction) => new()
    {
        Id = transaction.Id,
        ProductId = transaction.ProductId,
        ProductName = transaction.Product?.Name ?? string.Empty,
        SKU = transaction.Product?.SKU ?? string.Empty,
        UserId = transaction.UserId,
        UserName = transaction.User?.FullName,
        TransactionType = transaction.TransactionType.ToString(),
        Quantity = transaction.Quantity,
        QuantityBefore = transaction.QuantityBefore,
        QuantityAfter = transaction.QuantityAfter,
        ReferenceType = transaction.ReferenceType.ToString(),
        ReferenceId = transaction.ReferenceId,
        Notes = transaction.Notes,
        CreatedAt = transaction.CreatedAt
    };
}
