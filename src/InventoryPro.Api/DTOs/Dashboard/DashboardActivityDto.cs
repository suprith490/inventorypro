namespace InventoryPro.Api.DTOs.Dashboard;

/// <summary>Compact representation of a recent purchase or sale for the dashboard feed.</summary>
public class DashboardActivityDto
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string PartyName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public int TotalQuantity { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
}
