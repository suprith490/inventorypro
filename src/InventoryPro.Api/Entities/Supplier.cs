namespace InventoryPro.Api.Entities;

/// <summary>
/// Supplier / vendor that products are purchased from.
/// Maps to the "Suppliers" table.
/// </summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
