using System.ComponentModel.DataAnnotations;

namespace InventoryPro.Api.DTOs.Inventory;

/// <summary>
/// Manual stock adjustment (stock count correction, write-off, damage).
/// The service computes the delta from the current quantity and records it
/// in the inventory ledger as an Adjustment / ManualAdjustment entry.
/// </summary>
public class AdjustStockRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid product is required.")]
    public int ProductId { get; set; }

    /// <summary>The corrected absolute quantity on hand (not a delta).</summary>
    [Range(0, 1_000_000, ErrorMessage = "New quantity must be between 0 and 1,000,000.")]
    public int NewQuantity { get; set; }

    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string Reason { get; set; } = string.Empty;
}
