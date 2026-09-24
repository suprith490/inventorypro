namespace InventoryPro.Api.Common;

/// <summary>
/// Role names used by JWT claims and by [Authorize(Roles = ...)] attributes.
/// Keeping them in one place prevents "Admin" vs "admin" mistakes.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";

    /// <summary>Convenience value for endpoints both roles may call (stock movement).</summary>
    public const string AdminOrStaff = "Admin,Staff";

    public static IReadOnlyList<string> All { get; } = new[] { Admin, Staff };
}
