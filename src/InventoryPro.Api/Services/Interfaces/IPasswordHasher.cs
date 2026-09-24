namespace InventoryPro.Api.Services.Interfaces;

/// <summary>
/// Hashes and verifies passwords with a salted, slow algorithm (PBKDF2).
/// The Spring Security equivalent is a BCryptPasswordEncoder bean.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
