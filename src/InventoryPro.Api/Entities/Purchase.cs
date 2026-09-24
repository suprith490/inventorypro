namespace InventoryPro.Api.Entities;

/// <summary>
/// Stock-in document (goods received from a supplier).
/// Maps to the "Purchases" table.
/// </summary>
public class Purchase : BaseEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public int UserId { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Completed;

    public string? Notes { get; set; }

    // Navigation properties.
    public Supplier? Supplier { get; set; }
    public User? User { get; set; }
    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
