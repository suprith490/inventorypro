using InventoryPro.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPro.Api.Data.Configurations;

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(item => item.LineTotal)
            .HasPrecision(18, 2);

        builder.HasIndex(item => item.SaleId);
        builder.HasIndex(item => item.ProductId);

        builder.HasOne(item => item.Product)
            .WithMany(product => product.SaleItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
