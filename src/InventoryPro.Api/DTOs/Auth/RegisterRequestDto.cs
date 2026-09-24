using System.ComponentModel.DataAnnotations;

namespace InventoryPro.Api.DTOs.Auth;

/// <summary>
/// Public self-registration payload. New accounts are always created with the
/// Staff role; only an Admin can promote users (see the admin user endpoints).
/// </summary>
public class RegisterRequestDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email format is not valid.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;
}
