namespace InventoryPro.Api.Entities;

/// <summary>
/// One line of a Sale: how many units of a product and at what price.
/// Maps to the "SaleItems" table.
/// </summary>
public class SaleItem : BaseEntity
{
    public int SaleId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    // Navigation properties.
    public Sale? Sale { get; set; }
    public Product? Product { get; set; }
}
