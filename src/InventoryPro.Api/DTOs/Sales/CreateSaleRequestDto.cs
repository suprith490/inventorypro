using System.ComponentModel.DataAnnotations;

namespace InventoryPro.Api.DTOs.Sales;

/// <summary>
/// Payload for recording a Stock-Out (sales) document.
/// The service validates stock availability, creates the Sale header and items,
/// decreases product stock and writes one InventoryTransaction per line.
/// </summary>
public class CreateSaleRequestDto
{
    [StringLength(150, ErrorMessage = "Customer name cannot exceed 150 characters.")]
    public string? CustomerName { get; set; }

    public DateTime? SaleDate { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "At least one sale item is required.")]
    [MinLength(1, ErrorMessage = "At least one sale item is required.")]
    public List<CreateSaleItemDto> Items { get; set; } = new();
}

/// <summary>One line of a sale request.</summary>
public class CreateSaleItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid product is required.")]
    public int ProductId { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "Quantity must be between 1 and 1,000,000.")]
    public int Quantity { get; set; }

    /// <summary>
    /// Optional selling price override. When null the product's current UnitPrice is used.
    /// </summary>
    [Range(0, 9_999_999, ErrorMessage = "Unit price must be between 0 and 9,999,999.")]
    public decimal? UnitPrice { get; set; }
}
