using InventoryPro.Api.Services.Implementations;
using Xunit;

namespace InventoryPro.Api.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verify_ReturnsTrue_ForTheCorrectPassword()
    {
        var hash = _hasher.Hash("Admin@123");

        Assert.True(_hasher.Verify("Admin@123", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForTheWrongPassword()
    {
        var hash = _hasher.Hash("Admin@123");

        Assert.False(_hasher.Verify("WrongPassword!", hash));
    }

    [Fact]
    public void Hash_ProducesDifferentValues_ForTheSamePassword()
    {
        // A fresh salt on every hash means identical passwords never share a hash.
        var first = _hasher.Hash("Admin@123");
        var second = _hasher.Hash("Admin@123");

        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify("Admin@123", first));
        Assert.True(_hasher.Verify("Admin@123", second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    [InlineData("1.2")]
    public void Verify_ReturnsFalse_ForMalformedHashes(string malformedHash)
    {
        Assert.False(_hasher.Verify("Admin@123", malformedHash));
    }
}
