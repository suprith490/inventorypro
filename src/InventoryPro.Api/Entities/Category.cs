namespace InventoryPro.Api.Entities;

/// <summary>
/// Product category (e.g. "Electronics", "Stationery").
/// Maps to the "Categories" table.
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
