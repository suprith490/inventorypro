namespace InventoryPro.Api.Entities;

/// <summary>
/// Application user. Admin manages everything; Staff handles stock movement.
/// Maps to the "Users" table.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Staff;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties (like JPA @OneToMany collections).
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
