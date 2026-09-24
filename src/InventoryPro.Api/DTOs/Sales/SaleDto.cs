namespace InventoryPro.Api.DTOs.Sales;

/// <summary>A Stock-Out document with its line items.</summary>
public class SaleDto
{
    public int Id { get; set; }

    public string SaleNumber { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    public string? Notes { get; set; }

    public int TotalQuantity { get; set; }

    public IReadOnlyList<SaleItemDto> Items { get; set; } = Array.Empty<SaleItemDto>();

    public DateTime CreatedAt { get; set; }
}
