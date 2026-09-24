namespace InventoryPro.Api.Entities;

/// <summary>
/// Immutable audit trail of every stock movement.
/// Written automatically whenever a purchase or sale changes stock,
/// and optionally for manual adjustments.
/// Maps to the "InventoryTransactions" table.
/// </summary>
public class InventoryTransaction : BaseEntity
{
    public int ProductId { get; set; }

    public int? UserId { get; set; }

    public TransactionType TransactionType { get; set; }

    /// <summary>Absolute quantity moved (direction comes from TransactionType).</summary>
    public int Quantity { get; set; }

    public int QuantityBefore { get; set; }

    public int QuantityAfter { get; set; }

    public ReferenceType ReferenceType { get; set; } = ReferenceType.ManualAdjustment;

    /// <summary>Id of the Purchase or Sale that caused the movement (null for manual).</summary>
    public int? ReferenceId { get; set; }

    public string? Notes { get; set; }

    // Navigation properties.
    public Product? Product { get; set; }
    public User? User { get; set; }
}
