namespace InventoryPro.Api.DTOs.Health;

/// <summary>
/// Data Transfer Object returned by the health endpoint.
/// DTOs keep entities out of the API surface (same role as Java records/DTOs).
/// </summary>
public class HealthStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}
