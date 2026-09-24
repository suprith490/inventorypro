using InventoryPro.Api.Common;

namespace InventoryPro.Api.DTOs.Products;

/// <summary>
/// Search / filter / sort / pagination options for the product list.
/// Bound automatically from the query string: ?search=mouse&amp;categoryId=1&amp;sortBy=price&amp;sortDescending=true
/// </summary>
public class ProductQueryParameters : PaginationQueryParameters
{
    /// <summary>Free-text search across name, SKU and description.</summary>
    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public int? SupplierId { get; set; }

    /// <summary>When true only products at/below their reorder level are returned.</summary>
    public bool LowStockOnly { get; set; }

    /// <summary>Include inactive (soft-deleted) products. Admin screens only.</summary>
    public bool IncludeInactive { get; set; }

    /// <summary>name | sku | price | stock | created</summary>
    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
}
