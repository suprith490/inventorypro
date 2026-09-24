using InventoryPro.Api.Common;

namespace InventoryPro.Api.DTOs.Purchases;

/// <summary>Filter/pagination options for the purchase history endpoint.</summary>
public class PurchaseQueryParameters : PaginationQueryParameters
{
    public int? SupplierId { get; set; }

    public int? UserId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    /// <summary>Free-text search on purchase number, supplier name or user name.</summary>
    public string? Search { get; set; }

    public bool SortDescending { get; set; } = true;
}
