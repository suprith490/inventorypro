using InventoryPro.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPro.Api.Data.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.HasKey(sale => sale.Id);

        builder.Property(sale => sale.SaleNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sale => sale.SaleDate)
            .IsRequired();

        builder.Property(sale => sale.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(sale => sale.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(sale => sale.CustomerName)
            .HasMaxLength(150);

        builder.Property(sale => sale.Notes)
            .HasMaxLength(500);

        builder.HasIndex(sale => sale.SaleNumber).IsUnique();
        builder.HasIndex(sale => sale.SaleDate);

        builder.HasOne(sale => sale.User)
            .WithMany(user => user.Sales)
            .HasForeignKey(sale => sale.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sale => sale.Items)
            .WithOne(item => item.Sale)
            .HasForeignKey(item => item.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
