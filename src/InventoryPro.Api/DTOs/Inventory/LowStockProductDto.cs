namespace InventoryPro.Api.DTOs.Inventory;

/// <summary>A product whose stock is at or below its reorder level.</summary>
public class LowStockProductDto
{
    public int ProductId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int QuantityInStock { get; set; }

    public int ReorderLevel { get; set; }

    /// <summary>Suggested quantity to order back up to a healthy level.</summary>
    public int SuggestedReorderQuantity { get; set; }

    public bool IsOutOfStock { get; set; }
}
