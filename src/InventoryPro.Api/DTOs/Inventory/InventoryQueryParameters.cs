using InventoryPro.Api.Common;
using InventoryPro.Api.Entities;

namespace InventoryPro.Api.DTOs.Inventory;

/// <summary>Filter/pagination options for the inventory transaction ledger.</summary>
public class InventoryQueryParameters : PaginationQueryParameters
{
    public int? ProductId { get; set; }

    public int? UserId { get; set; }

    public TransactionType? TransactionType { get; set; }

    public ReferenceType? ReferenceType { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    /// <summary>Free-text search on product name, SKU or notes.</summary>
    public string? Search { get; set; }

    public bool SortDescending { get; set; } = true;
}
