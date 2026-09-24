using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.DTOs.Categories;
using InventoryPro.Api.DTOs.Suppliers;
using InventoryPro.Api.Services.Implementations;
using Xunit;

namespace InventoryPro.Api.Tests;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsName_AndPersists()
    {
        using var db = new TestDatabase();
        var service = new CategoryService(db.Context);

        var category = await service.CreateAsync(new CreateCategoryRequestDto
        {
            Name = "  Furniture  ",
            Description = "  Office furniture  "
        });

        Assert.Equal("Furniture", category.Name);
        Assert.Equal("Office furniture", category.Description);
        Assert.True(category.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ThrowsConflict_ForDuplicateName()
    {
        using var db = new TestDatabase();
        var service = new CategoryService(db.Context);
        await service.CreateAsync(new CreateCategoryRequestDto { Name = "Furniture" });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateCategoryRequestDto { Name = "Furniture" }));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsConflict_WhenCategoryIsInUse()
    {
        using var db = new TestDatabase();
        var seed = await db.SeedBaselineAsync();
        var service = new CategoryService(db.Context);

        // SeedBaselineAsync assigns two active products to this category.
        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(seed.Category.Id));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_UnusedCategory()
    {
        using var db = new TestDatabase();
        var service = new CategoryService(db.Context);
        var category = await service.CreateAsync(new CreateCategoryRequestDto { Name = "Unused" });

        await service.DeleteAsync(category.Id);

        var activeOnly = await service.GetAllAsync(includeInactive: false);
        var withInactive = await service.GetAllAsync(includeInactive: true);

        Assert.DoesNotContain(activeOnly, c => c.Id == category.Id);
        Assert.Contains(withInactive, c => c.Id == category.Id && !c.IsActive);
    }
}

public class SupplierServiceTests
{
    [Fact]
    public async Task CreateAsync_ThrowsConflict_ForDuplicateName()
    {
        using var db = new TestDatabase();
        var service = new SupplierService(db.Context);
        await service.CreateAsync(new CreateSupplierRequestDto { Name = "Acme" });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateSupplierRequestDto { Name = "Acme" }));
    }
}
