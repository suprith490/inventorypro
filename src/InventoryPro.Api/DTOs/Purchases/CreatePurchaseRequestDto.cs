using System.ComponentModel.DataAnnotations;

namespace InventoryPro.Api.DTOs.Purchases;

/// <summary>
/// Payload for recording a Stock-In (goods received) document.
/// The service creates the Purchase header, its items, updates product stock
/// and writes one InventoryTransaction per line - all in one database transaction.
/// </summary>
public class CreatePurchaseRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid supplier is required.")]
    public int SupplierId { get; set; }

    public DateTime? PurchaseDate { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "At least one purchase item is required.")]
    [MinLength(1, ErrorMessage = "At least one purchase item is required.")]
    public List<CreatePurchaseItemDto> Items { get; set; } = new();
}

/// <summary>One line of a purchase request.</summary>
public class CreatePurchaseItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid product is required.")]
    public int ProductId { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "Quantity must be between 1 and 1,000,000.")]
    public int Quantity { get; set; }

    [Range(0, 9_999_999, ErrorMessage = "Unit cost must be between 0 and 9,999,999.")]
    public decimal UnitCost { get; set; }
}
