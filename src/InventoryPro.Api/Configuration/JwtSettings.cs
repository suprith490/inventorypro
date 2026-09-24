namespace InventoryPro.Api.Configuration;

/// <summary>
/// Strongly-typed representation of the "Jwt" section in appsettings.json.
/// Equivalent to a Spring @ConfigurationProperties("jwt") class.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
