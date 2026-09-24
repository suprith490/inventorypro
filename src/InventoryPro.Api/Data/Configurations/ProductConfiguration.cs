using InventoryPro.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPro.Api.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);

        builder.Property(product => product.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(product => product.Description)
            .HasMaxLength(1000);

        builder.Property(product => product.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(product => product.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(product => product.QuantityInStock)
            .HasDefaultValue(0);

        builder.Property(product => product.ReorderLevel)
            .HasDefaultValue(10);

        builder.Property(product => product.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(product => product.SKU).IsUnique();
        builder.HasIndex(product => product.Name);
        builder.HasIndex(product => new { product.CategoryId, product.IsActive });

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(product => product.Supplier)
            .WithMany(supplier => supplier.Products)
            .HasForeignKey(product => product.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
