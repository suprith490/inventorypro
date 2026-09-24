namespace InventoryPro.Api.Entities;

/// <summary>
/// A stock item. QuantityInStock is updated automatically by purchases and sales.
/// Maps to the "Products" table.
/// </summary>
public class Product : BaseEntity
{
    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public int? SupplierId { get; set; }

    /// <summary>Selling price.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Purchase/landed cost.</summary>
    public decimal CostPrice { get; set; }

    public int QuantityInStock { get; set; }

    /// <summary>When stock falls to or below this level a low-stock alert is raised.</summary>
    public int ReorderLevel { get; set; } = 10;

    public bool IsActive { get; set; } = true;

    // Navigation properties.
    public Category? Category { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
