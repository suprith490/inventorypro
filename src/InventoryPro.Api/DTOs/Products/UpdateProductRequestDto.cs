using System.ComponentModel.DataAnnotations;

namespace InventoryPro.Api.DTOs.Products;

/// <summary>
/// Catalog fields only. Stock quantity is intentionally NOT editable here:
/// it changes only through purchases, sales and inventory adjustments so the
/// audit trail stays trustworthy.
/// </summary>
public class UpdateProductRequestDto
{
    [Required(ErrorMessage = "SKU is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "SKU must be between 2 and 50 characters.")]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A valid category is required.")]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Supplier id must be a positive number.")]
    public int? SupplierId { get; set; }

    [Range(0, 9999999, ErrorMessage = "Unit price must be between 0 and 9,999,999.")]
    public decimal UnitPrice { get; set; }

    [Range(0, 9999999, ErrorMessage = "Cost price must be between 0 and 9,999,999.")]
    public decimal CostPrice { get; set; }

    [Range(0, 1000000, ErrorMessage = "Reorder level must be between 0 and 1,000,000.")]
    public int ReorderLevel { get; set; } = 10;

    public bool IsActive { get; set; } = true;
}
