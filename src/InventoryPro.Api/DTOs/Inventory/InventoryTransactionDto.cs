namespace InventoryPro.Api.DTOs.Inventory;

/// <summary>
/// Read model for the immutable inventory audit trail.
/// One row is written for every purchase line, sale line and manual adjustment.
/// </summary>
public class InventoryTransactionDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int QuantityBefore { get; set; }

    public int QuantityAfter { get; set; }

    public string ReferenceType { get; set; } = string.Empty;

    public int? ReferenceId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}
