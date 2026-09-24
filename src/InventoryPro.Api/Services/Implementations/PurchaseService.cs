using InventoryPro.Api.Common;
using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.Data;
using InventoryPro.Api.DTOs.Purchases;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Services.Implementations;

/// <summary>
/// Records Stock-In documents. Creating a purchase:
///   1. validates the supplier and every product line,
///   2. writes the Purchase + PurchaseItems,
///   3. increases each product's QuantityInStock,
///   4. appends one InventoryTransaction per line for the audit trail.
/// All of it runs inside a single database transaction, so a failure anywhere
/// rolls the whole document back (like @Transactional on a Spring service method).
/// </summary>
public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _context;

    public PurchaseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<PurchaseDto>> GetPagedAsync(PurchaseQueryParameters parameters)
    {
        var query = _context.Purchases
            .AsNoTracking()
            .Include(purchase => purchase.Supplier)
            .Include(purchase => purchase.User)
            .Include(purchase => purchase.Items)
                .ThenInclude(item => item.Product)
            .AsQueryable();

        if (parameters.SupplierId.HasValue)
        {
            query = query.Where(purchase => purchase.SupplierId == parameters.SupplierId.Value);
        }

        if (parameters.UserId.HasValue)
        {
            query = query.Where(purchase => purchase.UserId == parameters.UserId.Value);
        }

        if (parameters.From.HasValue)
        {
            query = query.Where(purchase => purchase.PurchaseDate >= parameters.From.Value);
        }

        if (parameters.To.HasValue)
        {
            // Treat "to" as an inclusive day by comparing against the next midnight.
            var exclusiveEnd = parameters.To.Value.Date.AddDays(1);
            query = query.Where(purchase => purchase.PurchaseDate < exclusiveEnd);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.Trim().ToLowerInvariant();
            query = query.Where(purchase =>
                purchase.PurchaseNumber.ToLower().Contains(term) ||
                purchase.Supplier!.Name.ToLower().Contains(term) ||
                purchase.User!.FullName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        query = parameters.SortDescending
            ? query.OrderByDescending(purchase => purchase.PurchaseDate).ThenByDescending(purchase => purchase.Id)
            : query.OrderBy(purchase => purchase.PurchaseDate).ThenBy(purchase => purchase.Id);

        var purchases = await query
            .Skip(parameters.Skip)
            .Take(parameters.PageSize)
            .ToListAsync();

        return PagedResult<PurchaseDto>.Create(
            purchases.Select(MapToDto).ToList(),
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }

    public async Task<PurchaseDto> GetByIdAsync(int id)
    {
        var purchase = await _context.Purchases
            .AsNoTracking()
            .Include(candidate => candidate.Supplier)
            .Include(candidate => candidate.User)
            .Include(candidate => candidate.Items)
                .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (purchase is null)
        {
            throw new NotFoundException($"Purchase with id {id} was not found.");
        }

        return MapToDto(purchase);
    }

    public async Task<PurchaseDto> CreateAsync(CreatePurchaseRequestDto request, int userId)
    {
        var supplier = await _context.Suppliers
            .SingleOrDefaultAsync(candidate => candidate.Id == request.SupplierId && candidate.IsActive);

        if (supplier is null)
        {
            throw new BadRequestException(
                $"Supplier with id {request.SupplierId} does not exist or is inactive.");
        }

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
                    $"Product '{product.Name}' is inactive and cannot be purchased.");
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var purchaseDate = request.PurchaseDate ?? DateTime.UtcNow;

            var purchase = new Purchase
            {
                PurchaseNumber = await GeneratePurchaseNumberAsync(purchaseDate),
                SupplierId = supplier.Id,
                UserId = userId,
                PurchaseDate = purchaseDate,
                Status = PurchaseStatus.Completed,
                Notes = NormalizeText(request.Notes),
                Items = new List<PurchaseItem>()
            };

            var ledgerEntries = new List<InventoryTransaction>();
            decimal totalAmount = 0m;

            foreach (var item in request.Items)
            {
                var product = products[item.ProductId];
                var lineTotal = Math.Round(item.UnitCost * item.Quantity, 2);
                totalAmount += lineTotal;

                purchase.Items.Add(new PurchaseItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    LineTotal = lineTotal
                });

                var quantityBefore = product.QuantityInStock;
                product.QuantityInStock = quantityBefore + item.Quantity;

                ledgerEntries.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    UserId = userId,
                    TransactionType = TransactionType.StockIn,
                    Quantity = item.Quantity,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = product.QuantityInStock,
                    ReferenceType = ReferenceType.Purchase,
                    Notes = $"Stock in via purchase {purchase.PurchaseNumber}"
                });
            }

            purchase.TotalAmount = totalAmount;
            _context.Purchases.Add(purchase);

            // First save creates the IDs for the purchase; stock changes are saved too.
            await _context.SaveChangesAsync();

            // The ledger needs the generated purchase id.
            foreach (var entry in ledgerEntries)
            {
                entry.ReferenceId = purchase.Id;
                _context.InventoryTransactions.Add(entry);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdAsync(purchase.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Builds a human-friendly sequential number such as PO-20260924-0001,
    /// restarting the counter every day.
    /// </summary>
    private async Task<string> GeneratePurchaseNumberAsync(DateTime date)
    {
        var prefix = $"PO-{date:yyyyMMdd}-";

        var existingNumbers = await _context.Purchases
            .Where(purchase => purchase.PurchaseNumber.StartsWith(prefix))
            .Select(purchase => purchase.PurchaseNumber)
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

    private static PurchaseDto MapToDto(Purchase purchase) => new()
    {
        Id = purchase.Id,
        PurchaseNumber = purchase.PurchaseNumber,
        SupplierId = purchase.SupplierId,
        SupplierName = purchase.Supplier?.Name ?? string.Empty,
        UserId = purchase.UserId,
        UserName = purchase.User?.FullName ?? string.Empty,
        PurchaseDate = purchase.PurchaseDate,
        TotalAmount = purchase.TotalAmount,
        Status = purchase.Status.ToString(),
        Notes = purchase.Notes,
        TotalQuantity = purchase.Items.Sum(item => item.Quantity),
        CreatedAt = purchase.CreatedAt,
        Items = purchase.Items
            .OrderBy(item => item.Id)
            .Select(item => new PurchaseItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? string.Empty,
                SKU = item.Product?.SKU ?? string.Empty,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                LineTotal = item.LineTotal
            })
            .ToList()
    };
}
