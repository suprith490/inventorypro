using System.ComponentModel.DataAnnotations;
using InventoryPro.Api.Entities;

namespace InventoryPro.Api.DTOs.Users;

public class UpdateUserRoleRequestDto
{
    [Required(ErrorMessage = "Role is required.")]
    [EnumDataType(typeof(UserRole), ErrorMessage = "Role must be either 'Admin' or 'Staff'.")]
    public UserRole Role { get; set; }
}
