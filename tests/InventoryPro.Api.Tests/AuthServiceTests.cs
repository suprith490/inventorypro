using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.DTOs.Auth;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Services.Implementations;
using InventoryPro.Api.Services.Interfaces;
using Xunit;

namespace InventoryPro.Api.Tests;

/// <summary>Deterministic stand-in for the real JWT service so auth logic is tested in isolation.</summary>
internal sealed class FakeJwtTokenService : IJwtTokenService
{
    public JwtTokenResult GenerateToken(User user)
        => new($"fake-token-for-{user.Id}", DateTime.UtcNow.AddHours(1));
}

public class AuthServiceTests
{
    private static AuthService CreateService(TestDatabase db)
        => new(db.Context, new PasswordHasher(), new FakeJwtTokenService());

    [Fact]
    public async Task RegisterAsync_CreatesStaffAccount_AndReturnsToken()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            FullName = "Jane Doe",
            Email = "Jane.Doe@Example.com",
            Password = "Secret123"
        });

        Assert.Equal("Staff", result.User.Role);
        Assert.Equal("jane.doe@example.com", result.User.Email); // normalized to lowercase
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task RegisterAsync_ThrowsConflict_WhenEmailAlreadyExists()
    {
        using var db = new TestDatabase();
        await db.SeedBaselineAsync(); // already contains admin@test.local
        var service = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(new RegisterRequestDto
        {
            FullName = "Duplicate",
            Email = "ADMIN@TEST.LOCAL",
            Password = "Secret123"
        }));
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_ForValidCredentials()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);

        await service.RegisterAsync(new RegisterRequestDto
        {
            FullName = "Jane Doe",
            Email = "jane@example.com",
            Password = "Secret123"
        });

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email = "jane@example.com",
            Password = "Secret123"
        });

        Assert.Equal("jane@example.com", result.User.Email);
        Assert.StartsWith("fake-token-for-", result.Token);
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorized_ForWrongPassword()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        await service.RegisterAsync(new RegisterRequestDto
        {
            FullName = "Jane Doe",
            Email = "jane@example.com",
            Password = "Secret123"
        });

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(new LoginRequestDto
        {
            Email = "jane@example.com",
            Password = "WrongPassword"
        }));
    }

    [Fact]
    public async Task LoginAsync_ThrowsForbidden_ForDeactivatedAccount()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);
        var registered = await service.RegisterAsync(new RegisterRequestDto
        {
            FullName = "Jane Doe",
            Email = "jane@example.com",
            Password = "Secret123"
        });

        var user = await db.Context.Users.FindAsync(registered.User.Id);
        user!.IsActive = false;
        await db.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.LoginAsync(new LoginRequestDto
        {
            Email = "jane@example.com",
            Password = "Secret123"
        }));
    }

    [Fact]
    public async Task GetProfileAsync_ThrowsNotFound_WhenUserMissing()
    {
        using var db = new TestDatabase();
        var service = CreateService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetProfileAsync(12345));
    }
}
