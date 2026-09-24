namespace InventoryPro.Api.Entities;

/// <summary>
/// Base class for every database entity. Mirrors a JPA @MappedSuperclass.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
