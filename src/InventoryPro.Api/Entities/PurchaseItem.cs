namespace InventoryPro.Api.Entities;

/// <summary>
/// One line of a Purchase: how many units of a product and at what cost.
/// Maps to the "PurchaseItems" table.
/// </summary>
public class PurchaseItem : BaseEntity
{
    public int PurchaseId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal LineTotal { get; set; }

    // Navigation properties.
    public Purchase? Purchase { get; set; }
    public Product? Product { get; set; }
}
