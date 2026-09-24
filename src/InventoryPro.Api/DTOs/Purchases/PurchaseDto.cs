namespace InventoryPro.Api.DTOs.Purchases;

/// <summary>A Stock-In document with its line items.</summary>
public class PurchaseDto
{
    public int Id { get; set; }

    public string PurchaseNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public int TotalQuantity { get; set; }

    public IReadOnlyList<PurchaseItemDto> Items { get; set; } = Array.Empty<PurchaseItemDto>();

    public DateTime CreatedAt { get; set; }
}
