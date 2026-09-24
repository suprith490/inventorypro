using System.ComponentModel.DataAnnotations;

namespace InventoryPro.Api.DTOs.Suppliers;

public class UpdateSupplierRequestDto
{
    [Required(ErrorMessage = "Supplier name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Supplier name must be between 2 and 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Supplier email format is not valid.")]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters.")]
    public string? Phone { get; set; }

    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}
