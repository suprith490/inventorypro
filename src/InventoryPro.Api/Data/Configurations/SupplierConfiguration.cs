using InventoryPro.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPro.Api.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(supplier => supplier.Id);

        builder.Property(supplier => supplier.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(supplier => supplier.Email)
            .HasMaxLength(256);

        builder.Property(supplier => supplier.Phone)
            .HasMaxLength(30);

        builder.Property(supplier => supplier.Address)
            .HasMaxLength(300);

        builder.Property(supplier => supplier.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(supplier => supplier.Name);

        builder.HasMany(supplier => supplier.Products)
            .WithOne(product => product.Supplier)
            .HasForeignKey(product => product.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(supplier => supplier.Purchases)
            .WithOne(purchase => purchase.Supplier)
            .HasForeignKey(purchase => purchase.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
