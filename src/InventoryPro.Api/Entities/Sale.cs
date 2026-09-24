namespace InventoryPro.Api.Entities;

/// <summary>
/// Stock-out document (goods sold to a customer).
/// Maps to the "Sales" table.
/// </summary>
public class Sale : BaseEntity
{
    public string SaleNumber { get; set; } = string.Empty;

    public int UserId { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    public string? CustomerName { get; set; }

    public string? Notes { get; set; }

    // Navigation properties.
    public User? User { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
