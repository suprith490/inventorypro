namespace InventoryPro.Api.DTOs.Purchases;

/// <summary>One line of a Stock-In document.</summary>
public class PurchaseItemDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal LineTotal { get; set; }
}
