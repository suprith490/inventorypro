using InventoryPro.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPro.Api.Data.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");
        builder.HasKey(purchase => purchase.Id);

        builder.Property(purchase => purchase.PurchaseNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(purchase => purchase.PurchaseDate)
            .IsRequired();

        builder.Property(purchase => purchase.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(purchase => purchase.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(purchase => purchase.Notes)
            .HasMaxLength(500);

        builder.HasIndex(purchase => purchase.PurchaseNumber).IsUnique();
        builder.HasIndex(purchase => purchase.PurchaseDate);

        builder.HasOne(purchase => purchase.Supplier)
            .WithMany(supplier => supplier.Purchases)
            .HasForeignKey(purchase => purchase.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(purchase => purchase.User)
            .WithMany(user => user.Purchases)
            .HasForeignKey(purchase => purchase.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Line items belong to the document: delete the purchase, delete its lines.
        builder.HasMany(purchase => purchase.Items)
            .WithOne(item => item.Purchase)
            .HasForeignKey(item => item.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
