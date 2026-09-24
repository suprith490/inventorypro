using InventoryPro.Api.Common;

namespace InventoryPro.Api.DTOs.Sales;

/// <summary>Filter/pagination options for the sales history endpoint.</summary>
public class SaleQueryParameters : PaginationQueryParameters
{
    public int? UserId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    /// <summary>Free-text search on sale number, customer name or user name.</summary>
    public string? Search { get; set; }

    public bool SortDescending { get; set; } = true;
}
